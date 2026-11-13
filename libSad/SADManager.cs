using SadViewer.Models;
using System.IO;
using System.Text.Json;          // o Newtonsoft.Json
using static Simpro.Sad;

/// <summary>
/// SADManager - Control central del sistema SAD (Simpro Acquisition Device).
/// Adaptado a WPF / .NET.
/// </summary>
/// <remarks>
/// Singleton que encapsula el ciclo de vida del SDK SAD:
/// carga de configuración, apertura de la BD (HRL/GHT), apertura del puerto
/// serie y handshake con la electrónica.
/// </remarks>
/// <remarks>
/// ⚠️ ADVERTENCIA:
/// - La clase <see cref="SADConfig"/> definida al final de este archivo
///   DUPLICA la de <c>MainViewModel.cs</c>. Elimina una de las dos o
///   muévela a su propio archivo (error CS0101 en caso contrario).
/// - El singleton no es thread-safe. Si lo usas desde varios hilos,
///   considera envolverlo con <see cref="Lazy{T}"/>.
/// </remarks>
public sealed class SADManager
{
    #region Singleton

    // ⚠️ CS8618 si <Nullable>enable</Nullable>: campo no inicializado en ctor.
    //    Puedes declararlo como `SADManager? inst;` o inicializar a null explícitamente.
    private static SADManager? inst;

    /// <summary>Instancia única perezosa. No es atómica entre hilos.</summary>
    public static SADManager Instance => inst ??= new SADManager();

    #endregion

    #region Configuración

    /// <summary>Puerto COM configurado (se carga en <see cref="CargarConfiguracion"/>).</summary>
    public string COM { get; private set; } = "COM1";

    /// <summary>Velocidad en baudios configurada.</summary>
    public int RATE { get; private set; } = 115200;

    /// <summary>
    /// Carpeta (absoluta) donde residen HRL y GHT.
    /// Se resuelve contra <see cref="AppContext.BaseDirectory"/> si es relativa.
    /// </summary>
    public string DatabaseFolder { get; set; } =
        Path.Combine(AppContext.BaseDirectory, "SAD");

    /// <summary>Nombre del archivo HRL dentro de <see cref="DatabaseFolder"/>.</summary>
    public string HRLFile { get; set; } = "BS.hrl";

    /// <summary>Nombre del archivo GHT dentro de <see cref="DatabaseFolder"/>.</summary>
    public string GHTFile { get; set; } = "BS.ght";

    // ⚠️ CS8618: se inicializa en CargarConfiguracion(), no en ctor.
    private SADConfig config;

    /// <summary>
    /// Carga la configuración desde <c>SADConfig.json</c> (una sola vez por
    /// instancia, dado el uso de <c>??=</c>).
    /// </summary>
    private void CargarConfiguracion()
    {
        // Bug lógico: si ya se cargó, NO se relee el archivo aunque cambie en disco.
        config = CargarSADConfig();

        if (config == null)
        {
            Log("[SAD] No se encontró configuración SADConfig.");
            return;
        }

        COM = config.COM;
        RATE = config.RATE;
        DatabaseFolder = ResolvePath(config.DatabaseFolder);
        HRLFile = config.HRLFile;
        GHTFile = config.GHTFile;
    }

    /// <summary>
    /// Resuelve una ruta relativa contra <see cref="AppContext.BaseDirectory"/>.
    /// </summary>
    private static string ResolvePath(string p)
    {
        if (string.IsNullOrWhiteSpace(p)) return AppContext.BaseDirectory;
        return Path.IsPathRooted(p) ? p : Path.Combine(AppContext.BaseDirectory, p);
    }

    /// <summary>
    /// Lee <c>SADConfig.json</c> del directorio base. Devuelve un
    /// <see cref="SADConfig"/> con valores por defecto si no existe o falla.
    /// </summary>
    private SADConfig CargarSADConfig()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "SADConfig.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                if (json != null)
                    return JsonSerializer.Deserialize<SADConfig>(json, options: new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }
        catch (Exception ex)
        {
            Log("[SAD] Error leyendo SADConfig.json: " + ex.Message);

        }

        return new SADConfig();
    }

    #endregion

    #region Datos

    //    OpenConnection(), no en el ctor. Puedes usar `?` o `= null!`.
    /// <summary>Base de datos SAD abierta (HRL + GHT).</summary>
    public DataBase db { get; private set; }

    /// <summary>Puerto serie abierto para la comunicación con la electrónica.</summary>
    public SerialDevice serial { get; private set; }

    /// <summary>Instancia del SDK <c>Simpro.Sad</c>.</summary>
    public Simpro.Sad sad { get; private set; }

    #endregion

    #region Inicialización

    /// <summary>
    /// Ejecuta la secuencia completa de apertura: carga configuración,
    /// abre BD, abre puerto, inicializa el SDK y realiza el handshake.
    /// </summary>
    /// <returns><c>true</c> si la conexión quedó operativa.</returns>
    public bool OpenConnection()
    {
        try
        {
            CargarConfiguracion();

            sad = new Simpro.Sad();
            db = new DataBase();
            serial = new SerialDevice();

            string hrl = Path.Combine(DatabaseFolder, HRLFile);
            string ght = Path.Combine(DatabaseFolder, GHTFile);

            if (!File.Exists(hrl)) { Log($"[SAD] No existe HRL: {hrl}"); return false; }
            if (!File.Exists(ght)) { Log($"[SAD] No existe GHT: {ght}"); return false; }

            if (!db.Open(hrl, ght))
            {
                Log("[SADManager] No fue posible abrir la base de datos.");
                return false;
            }

            if (!serial.Open(COM, RATE))
            {
                Log($"[SADManager] No fue posible abrir el puerto {COM}:{RATE}");
                return false;
            }

            if (sad.Init(db, serial) != 0)
            {
                Log("[SADManager] Error inicializando SADManager.");
                return false;
            }

            // Reintentos de handshake (máx. 3)
            const int maxAttempts = 3;
            for (int i = 0; i < maxAttempts && !sad.IsConnected(); i++)
                sad.Connect(3);

            if (!sad.IsConnected())
            {
                Log("[SADManager] No fue posible conectar con la electrónica.");
                return false;
            }

            // Permisos y comportamiento de notificación de cambios
            sad.SetPermission(Permission.AllowACK);
            sad.SetPermission(Permission.AllowAll);
            // ⚠️ Inconsistencia con MainViewModel: allí se usa BasedOnDelegates.
            //    Con Nothing, ningún OnPieceChangeDelegate será invocado.
            sad.ChangesBehavior = Behavior.Nothing;

            Log($"[SADManager] Electrónica inicializada correctamente ({COM}:{RATE})");
            return true;
        }
        catch (Exception ex)
        {
            Log("[SADManager] Excepción en OpenConnection: " + ex);
            return false;
        }
    }

    /// <summary>
    /// Cierra la conexión y libera los recursos del SDK, la BD y el puerto.
    /// </summary>
    public void CloseConnection()
    {
        try
        {
            sad?.Dispose();

            // ⚠️ Comprobación muerta/engañosa: tras Dispose() el estado
            //    "desconectado" ya no refleja si el cierre fue correcto.
            //    Considera loguear antes de Dispose o comprobar el retorno de Dispose si existe.
            if (sad != null && !sad.IsConnected())
                Log("[SADManager] Electrónica desconectada correctamente");
        }
        catch (Exception ex)
        {
            Log("[SADManager] Error en CloseConnection: " + ex.Message);
        }
        finally
        {
            sad = null;
            db = null;
            serial = null;
        }
    }

    #endregion

    #region Log

    /// <summary>
    /// Evento emitido por cada entrada de log.
    /// </summary>
    public event Action<string>? OnLog;

    /// <summary>Registra un mensaje en <see cref="OnLog"/> y en Debug.</summary>
    private void Log(string msg)
    {
        OnLog?.Invoke(msg);
        System.Diagnostics.Debug.WriteLine(msg);
    }

    #endregion
}