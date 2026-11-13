using SadViewer.Models;
using Simpro;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static Simpro.Sad;

namespace SadViewer.ViewModels
{
    /// <summary>
    /// ViewModel principal de la aplicación.
    /// </summary>
    /// <remarks>
    /// Agrupa:
    /// - Configuración de conexión (puerto, baudios, rutas de BD).
    /// - Estado de la conexión y contadores de piezas.
    /// - Datos (bloques y piezas) y su filtrado.
    /// - Log de eventos con filtros por nivel y texto.
    /// - Comandos expuestos a la vista.
    /// Toda la lógica pesada (I/O, apertura de puerto, conexión) se ejecuta en
    /// hilos de fondo; las actualizaciones de estado se marshalean a la UI con
    /// <see cref="InvokeUI(Action)"/>.
    /// </remarks>
    public class MainViewModel : INotifyPropertyChanged
    {
        // ==============================================================
        // CONEXIÓN
        // ==============================================================
        private string _comPort = "COM1";
        private string _baudRate = "115200";
        private string _hrlPath;
        private string _ghtPath;
        private string _status = "Desconectado";

        /// <summary>Puerto COM seleccionado para la conexión.</summary>
        public string ComPort { get => _comPort; set => Set(ref _comPort, value); }

        /// <summary>Velocidad en baudios seleccionada.</summary>
        public string BaudRate { get => _baudRate; set => Set(ref _baudRate, value); }

        /// <summary>Ruta completa al archivo HRL de la base de datos.</summary>
        public string HrlPath { get => _hrlPath; set => Set(ref _hrlPath, value); }

        /// <summary>Nombre del archivo HRL sin extensión (usado al exportar).</summary>
        public string HrlFileNameWithoutExt => Path.GetFileNameWithoutExtension(HrlPath);

        /// <summary>Ruta completa al archivo GHT de la base de datos.</summary>
        public string GhtPath { get => _ghtPath; set => Set(ref _ghtPath, value); }

        /// <summary>Listado de puertos COM disponibles en el sistema.</summary>
        public ObservableCollection<string> AvailablePorts { get; } = new();

        /// <summary>Velocidades en baudios disponibles para el combo de selección.</summary>
        public ObservableCollection<string> AvailableBaunds { get; } =
            new(new[] { "9600", "19200", "38400", "57600", "115200", "230400" });

        /// <summary>
        /// Texto de estado de conexión ("Desconectado", "Conectando...", etc.).
        /// Al cambiar, dispara notificaciones para las propiedades derivadas
        /// (color, badge y texto resumido).
        /// </summary>
        public string Status
        {
            get => _status;
            set
            {
                if (Set(ref _status, value))
                {
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(StatusBadgeBg));
                    OnPropertyChanged(nameof(StatusBadgeFg));
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        /// <summary>Texto corto derivado de <see cref="Status"/> para mostrar en la UI.</summary>
        public string StatusText =>
            Status.StartsWith("Conectando", StringComparison.OrdinalIgnoreCase) ? "Conectando…" :
            Status.StartsWith("Conectado", StringComparison.OrdinalIgnoreCase) ? "Conectado" :
            Status.StartsWith("Error", StringComparison.OrdinalIgnoreCase) ? "Error" :
                                                                                  "Desconectado";

        /// <summary>Color del indicador según el estado (verde/rojo/azul/amarillo).</summary>
        public Brush StatusColor =>
            Status.StartsWith("Conectado", StringComparison.OrdinalIgnoreCase) ? new SolidColorBrush(Color.FromRgb(0x3B, 0xA5, 0x5D)) :
            Status.StartsWith("Error", StringComparison.OrdinalIgnoreCase) ? new SolidColorBrush(Color.FromRgb(0xED, 0x42, 0x45)) :
            Status.StartsWith("Conectando", StringComparison.OrdinalIgnoreCase) ? new SolidColorBrush(Color.FromRgb(0x58, 0x65, 0xF2)) :
                                                                                  new SolidColorBrush(Color.FromRgb(0xF0, 0xB2, 0x32));

        /// <summary>Color de fondo del badge de estado (variante oscura del color principal).</summary>
        public Brush StatusBadgeBg =>
            Status.StartsWith("Conectado", StringComparison.OrdinalIgnoreCase) ? new SolidColorBrush(Color.FromRgb(0x1F, 0x3A, 0x2B)) :
            Status.StartsWith("Error", StringComparison.OrdinalIgnoreCase) ? new SolidColorBrush(Color.FromRgb(0x3A, 0x1F, 0x22)) :
            Status.StartsWith("Conectando", StringComparison.OrdinalIgnoreCase) ? new SolidColorBrush(Color.FromRgb(0x1F, 0x24, 0x3A)) :
                                                                                  new SolidColorBrush(Color.FromRgb(0x3A, 0x33, 0x1F));

        /// <summary>Color del texto del badge (reutiliza el color principal).</summary>
        public Brush StatusBadgeFg => StatusColor;

        // ==============================================================
        // ESTADO
        // ==============================================================
        private bool _isConnecting;

        /// <summary>Indica que hay una conexión en curso; bloquea comandos sensibles.</summary>
        public bool IsConnecting
        {
            get => _isConnecting;
            private set { if (Set(ref _isConnecting, value)) RaiseStateChanged(); }
        }

        private bool _isDbLoaded;

        /// <summary>Indica que la base de datos HRL/GHT fue cargada correctamente.</summary>
        public bool IsDbLoaded
        {
            get => _isDbLoaded;
            private set { if (Set(ref _isDbLoaded, value)) { OnPropertyChanged(nameof(IsConnected)); RaiseStateChanged(); } }
        }

        /// <summary>Estado real de conexión con la electrónica (delegado al SDK).</summary>
        public bool IsConnected => _sad != null && _sad.IsConnected();

        private string _lastRefreshText = "—";

        /// <summary>Hora del último refresco de valores (manual o por evento).</summary>
        public string LastRefreshText { get => _lastRefreshText; private set => Set(ref _lastRefreshText, value); }

        private int _visibleCount;

        /// <summary>Cantidad de piezas visibles tras aplicar el filtro.</summary>
        public int VisibleCount { get => _visibleCount; private set => Set(ref _visibleCount, value); }

        private int _totalCount;

        /// <summary>Cantidad total de piezas cargadas en la BD.</summary>
        public int TotalCount { get => _totalCount; private set => Set(ref _totalCount, value); }

        private int _errorCount;

        /// <summary>Contador acumulado de errores registrados en el log.</summary>
        public int ErrorCount { get => _errorCount; private set => Set(ref _errorCount, value); }

        // ==============================================================
        // DATOS
        // ==============================================================
        /// <summary>Listado de bloques cargados desde la BD.</summary>
        public ObservableCollection<BlockViewModel> Blocks { get; } = new();

        /// <summary>Piezas del bloque seleccionado, ya filtradas.</summary>
        public ObservableCollection<PieceViewModel> PiecesView { get; } = new();

        private BlockViewModel _selectedBlock;

        /// <summary>Bloque actualmente seleccionado; al cambiar, reaplica el filtro.</summary>
        public BlockViewModel SelectedBlock
        {
            get => _selectedBlock;
            set { if (Set(ref _selectedBlock, value)) ApplyFilter(); }
        }

        private PieceViewModel _selectedPiece;

        /// <summary>Pieza actualmente seleccionada (para panel de detalle).</summary>
        public PieceViewModel SelectedPiece
        {
            get => _selectedPiece;
            set => Set(ref _selectedPiece, value);
        }

        private string _filter = "";

        /// <summary>Texto de filtrado de piezas por nombre.</summary>
        public string Filter
        {
            get => _filter;
            set { if (Set(ref _filter, value)) ApplyFilter(); }
        }

        // ==============================================================
        // LOG
        // ==============================================================
        /// <summary>Colección completa de líneas de log (máx. 2000).</summary>
        public ObservableCollection<LogLine> LogLines { get; } = new();

        private string _logFilter = "";

        /// <summary>Texto de filtrado del log por contenido del mensaje.</summary>
        public string LogFilter
        {
            get => _logFilter;
            set { if (Set(ref _logFilter, value)) OnPropertyChanged(nameof(FilteredLogLines)); }
        }

        private bool _showInfo = true, _showOk = true, _showWarn = true, _showErr = true;

        /// <summary>Mostrar entradas de nivel Info.</summary>
        public bool ShowInfo { get => _showInfo; set { if (Set(ref _showInfo, value)) OnPropertyChanged(nameof(FilteredLogLines)); } }

        /// <summary>Mostrar entradas de nivel Ok.</summary>
        public bool ShowOk { get => _showOk; set { if (Set(ref _showOk, value)) OnPropertyChanged(nameof(FilteredLogLines)); } }

        /// <summary>Mostrar entradas de nivel Warn.</summary>
        public bool ShowWarn { get => _showWarn; set { if (Set(ref _showWarn, value)) OnPropertyChanged(nameof(FilteredLogLines)); } }

        /// <summary>Mostrar entradas de nivel Error.</summary>
        public bool ShowErr { get => _showErr; set { if (Set(ref _showErr, value)) OnPropertyChanged(nameof(FilteredLogLines)); } }

        /// <summary>
        /// Vista filtrada del log: aplica filtros de nivel y de texto.
        /// Se recalcula cada vez que cambia un filtro o se agrega una línea.
        /// </summary>
        public System.Collections.IEnumerable FilteredLogLines =>
            LogLines.Where(l =>
                (l.Level == LogLevel.Info && ShowInfo) ||
                (l.Level == LogLevel.Ok && ShowOk) ||
                (l.Level == LogLevel.Warn && ShowWarn) ||
                (l.Level == LogLevel.Error && ShowErr))
            .Where(l => string.IsNullOrWhiteSpace(LogFilter) ||
                        l.Message.IndexOf(LogFilter, StringComparison.OrdinalIgnoreCase) >= 0);

        // ==============================================================
        // COMANDOS
        // ==============================================================
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SaveConfigCommand { get; }
        public ICommand BrowseHrlCommand { get; }
        public ICommand BrowseGhtCommand { get; }
        public ICommand LoadDatabaseCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand CopyLogCommand { get; }
        public ICommand RefreshPortsCommand { get; }
        public ICommand ClearFilterCommand { get; }
        public ICommand ExportarCommand { get; }

        // ==============================================================
        // ESTADO INTERNO
        // ==============================================================
        private Sad.DataBase _db;          // Base de datos SAD abierta
        private Sad.SerialDevice _serial;  // Puerto serie abierto
        private Sad _sad;                  // Instancia del SDK SAD
        private readonly Dictionary<IntPtr, BlockViewModel> _blockByHandle = new(); // Índice por handle

        // ==============================================================
        // CONSTRUCTOR
        // ==============================================================
        /// <summary>
        /// Inicializa comandos, carga la configuración, refresca puertos
        /// y lanza la carga inicial de la base de datos en segundo plano.
        /// </summary>
        public MainViewModel()
        {
            // CanExecute delimita cuándo puede ejecutarse cada comando
            ConnectCommand = new RelayCommand(_ => _ = ConnectAsync(),
                                                    _ => !IsConnecting && _sad == null);
            DisconnectCommand = new RelayCommand(_ => Disconnect(),
                                                    _ => !IsConnecting && _sad != null);
            RefreshCommand = new RelayCommand(_ => RefreshAllValues(), _ => IsDbLoaded);
            SaveConfigCommand = new RelayCommand(_ => SaveConfig());
            BrowseHrlCommand = new RelayCommand(_ => BrowseHrl(), _ => !IsConnecting);
            BrowseGhtCommand = new RelayCommand(_ => BrowseGht(), _ => !IsConnecting);
            LoadDatabaseCommand = new RelayCommand(_ => _ = LoadDatabaseAsync(), _ => !IsConnecting);
            ClearLogCommand = new RelayCommand(_ => LogLines.Clear());
            CopyLogCommand = new RelayCommand(_ => CopyLog());
            RefreshPortsCommand = new RelayCommand(_ => RefreshPorts());
            ClearFilterCommand = new RelayCommand(_ => Filter = "");
            ExportarCommand = new RelayCommand(_ =>
            {
                // La exportación requiere que la BD esté cargada
                if (_db == null)
                {
                    MessageBox.Show(
                        "No hay datos cargados para exportar.",
                        "Exportar",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Genera el código de piezas a partir de la BD y el nombre del HRL
                PieceCodeGenerator.Generate(db: _db, HrlFileNameWithoutExt);
            });

            CargarConfiguracion();
            RefreshPorts();

            //// Carga inicial de BD en background (fire-and-forget)
            //_ = LoadDatabaseAsync();
        }

        // ==============================================================
        // CONFIG
        // ==============================================================
        /// <summary>
        /// Carga la configuración (COM, baudios, rutas) desde <c>SADConfig.json</c>
        /// y registra en el log los valores resultantes.
        /// </summary>
        private void CargarConfiguracion()
        {
            var cfg = LoadConfigOrDefault();

            ComPort = string.IsNullOrWhiteSpace(cfg.COM) ? "COM1" : cfg.COM;
            BaudRate = cfg.RATE <= 0 ? "115200" : cfg.RATE.ToString();

            string basePath = ResolvePath(cfg.DatabaseFolder);
            HrlPath = Path.Combine(basePath, cfg.HRLFile ?? "BS.hrl");
            GhtPath = Path.Combine(basePath, cfg.GHTFile ?? "BS.ght");

            Log(LogLevel.Info, $"Config: {ComPort}@{BaudRate}");
            Log(LogLevel.Info, $"HRL: {HrlPath}");
            Log(LogLevel.Info, $"GHT: {GhtPath}");

            if (!File.Exists(HrlPath)) Log(LogLevel.Warn, "No existe el archivo HRL.");
            if (!File.Exists(GhtPath)) Log(LogLevel.Warn, "No existe el archivo GHT.");
        }

        /// <summary>
        /// Lee <c>SADConfig.json</c> desde el directorio de la app o desde
        /// <c>%APPDATA%\SadViewer</c>. Si falla o no existe, devuelve valores por defecto.
        /// </summary>
        private SADConfig LoadConfigOrDefault()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "SADConfig.json");

            // Fallback a AppData si no está junto al ejecutable
            if (!File.Exists(path))
            {
                var alt = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SadViewer", "SADConfig.json");
                if (File.Exists(alt)) path = alt;
            }

            if (!File.Exists(path))
            {
                Log(LogLevel.Warn, "SADConfig.json no encontrado. Valores por defecto.");
                return new SADConfig();
            }

            try
            {
                var json = File.ReadAllText(path);
                var cfg = JsonSerializer.Deserialize<SADConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,   // Ignora mayúsculas/minúsculas en claves
                    ReadCommentHandling = JsonCommentHandling.Skip, // Permite comentarios
                    AllowTrailingCommas = true             // Tolerar comas finales
                });

                if (cfg == null) throw new InvalidOperationException("JSON vacío.");

                // Valores por defecto si el JSON trae campos vacíos/inválidos
                cfg.COM ??= "COM1";
                cfg.RATE = cfg.RATE <= 0 ? 115200 : cfg.RATE;
                cfg.DatabaseFolder ??= "SAD";
                cfg.HRLFile ??= "BS.hrl";
                cfg.GHTFile ??= "BS.ght";

                Log(LogLevel.Ok, $"Config leída: {path}");
                return cfg;
            }
            catch (Exception ex)
            {
                Log(LogLevel.Warn, $"Error leyendo {path}: {ex.Message}. Valores por defecto.");
                return new SADConfig();
            }
        }

        /// <summary>
        /// Guarda la configuración actual en <c>SADConfig.json</c> junto al ejecutable.
        /// </summary>
        private void SaveConfig()
        {
            try
            {
                var cfg = new SADConfig
                {
                    COM = ComPort,
                    RATE = int.TryParse(BaudRate, out var r) ? r : 115200,
                    DatabaseFolder = Path.GetDirectoryName(HrlPath) ?? "SAD",
                    HRLFile = Path.GetFileName(HrlPath),
                    GHTFile = Path.GetFileName(GhtPath)
                };

                var path = Path.Combine(AppContext.BaseDirectory, "SADConfig.json");
                var json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);

                Log(LogLevel.Ok, $"Config guardada: {path}");
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Error guardando config: " + ex.Message);
            }
        }

        /// <summary>
        /// Resuelve una ruta relativa contra el directorio base de la aplicación.
        /// </summary>
        private static string ResolvePath(string p)
        {
            if (string.IsNullOrWhiteSpace(p)) return AppContext.BaseDirectory;
            return Path.IsPathRooted(p) ? p : Path.Combine(AppContext.BaseDirectory, p);
        }

        /// <summary>
        /// Enumera los puertos COM disponibles y los publica en <see cref="AvailablePorts"/>.
        /// </summary>
        private void RefreshPorts()
        {
            try
            {
                AvailablePorts.Clear();
                foreach (var p in SerialPort.GetPortNames().OrderBy(x => x))
                    AvailablePorts.Add(p);
                if (AvailablePorts.Count == 0)
                    Log(LogLevel.Warn, "No se detectaron puertos COM.");
                else
                    Log(LogLevel.Info, "Puertos detectados: " + string.Join(", ", AvailablePorts));
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "RefreshPorts: " + ex.Message);
            }
        }

        // ==============================================================
        // BUSCAR ARCHIVOS
        // ==============================================================
        /// <summary>
        /// Abre un diálogo para seleccionar el archivo HRL, guarda la ruta
        /// en la configuración y recarga la BD.
        /// </summary>
        private void BrowseHrl()
        {
            var path = PickFile("Seleccionar archivo HRL",
                                "SAD HRL (*.hrl)|*.hrl|Todos (*.*)|*.*", HrlPath);
            if (path == null) return;
            HrlPath = path;
            Log(LogLevel.Info, $"HRL seleccionado: {path}");
            SaveConfig();
            _ = LoadDatabaseAsync();
        }

        /// <summary>
        /// Abre un diálogo para seleccionar el archivo GHT, guarda la ruta
        /// en la configuración y recarga la BD.
        /// </summary>
        private void BrowseGht()
        {
            var path = PickFile("Seleccionar archivo GHT",
                                "SAD GHT (*.ght)|*.ght|Todos (*.*)|*.*", GhtPath);
            if (path == null) return;
            GhtPath = path;
            Log(LogLevel.Info, $"GHT seleccionado: {path}");
            SaveConfig();
            _ = LoadDatabaseAsync();
        }

        /// <summary>
        /// Muestra un <see cref="Microsoft.Win32.OpenFileDialog"/> y devuelve la ruta elegida.
        /// </summary>
        /// <returns>Ruta del archivo seleccionado, o <c>null</c> si se canceló.</returns>
        private static string PickFile(string title, string filter, string currentPath)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = title,
                Filter = filter,
                CheckFileExists = true,
                Multiselect = false
            };
            try
            {
                // Abre el diálogo en la carpeta del archivo actual, si existe
                if (!string.IsNullOrWhiteSpace(currentPath))
                {
                    var dir = Path.GetDirectoryName(currentPath);
                    if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                        dlg.InitialDirectory = dir;
                }
            }
            catch { }
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }

        // ==============================================================
        // CARGA DE BD
        // ==============================================================
        /// <summary>
        /// Carga la base de datos HRL/GHT en un hilo de fondo y puebla la vista.
        /// </summary>
        public async Task LoadDatabaseAsync()
        {
            if (IsConnecting) return;

            await Task.Run(() =>
            {
                try
                {
                    // Verifica que existan los archivos
                    if (!File.Exists(HrlPath)) { Log(LogLevel.Error, "HRL no existe: " + HrlPath); return; }
                    if (!File.Exists(GhtPath)) { Log(LogLevel.Error, "GHT no existe: " + GhtPath); return; }

                    var db = new Sad.DataBase();
                    if (!db.Open(HrlPath, GhtPath))
                    {
                        Log(LogLevel.Error, "No se pudo abrir la base de datos");
                        return;
                    }

                    // La UI debe actualizarse en el hilo principal
                    InvokeUI(() =>
                    {
                        _db = db;
                        PopulateFromDb(db);
                        IsDbLoaded = true;
                        LastRefreshText = DateTime.Now.ToString("HH:mm:ss");
                        Log(LogLevel.Ok, $"Tabla poblada: {Blocks.Count} bloques / {TotalCount} piezas");
                    });
                }
                catch (Exception ex)
                {
                    Log(LogLevel.Error, "LoadDatabaseAsync: " + ex);
                }
            });
        }

        /// <summary>
        /// Limpia las colecciones y las repuebla a partir de la base de datos.
        /// </summary>
        private void PopulateFromDb(Sad.DataBase db)
        {
            Blocks.Clear();
            PiecesView.Clear();
            _blockByHandle.Clear();
            SelectedBlock = null;
            SelectedPiece = null;

            int total = 0;
            foreach (var block in db.Blocks)
            {
                // Se pasa un provider de conexión y un logger a cada BlockViewModel
                var bvm = new BlockViewModel(block, () => IsConnected, msg => Log(LogLevel.Info, msg));
                Blocks.Add(bvm);
                _blockByHandle[block.Handle] = bvm;
                total += bvm.Pieces.Count;
            }
            TotalCount = total;

            // Selecciona el primer bloque por conveniencia
            if (Blocks.Count > 0)
                SelectedBlock = Blocks[0];
        }

        // ==============================================================
        // CONEXIÓN ASYNC
        // ==============================================================
        /// <summary>
        /// Establece la conexión serie con la electrónica en segundo plano.
        /// </summary>
        public async Task ConnectAsync()
        {
            if (IsConnecting) return;

            IsConnecting = true;
            Status = "Conectando...";
            Log(LogLevel.Info, "Iniciando conexión...");

            bool ok = false;
            try
            {
                // Trabajo pesado en hilo de fondo
                ok = await Task.Run(() => DoConnectHeavy());

                if (ok)
                {
                    if (!IsDbLoaded && _db != null)
                    {
                        PopulateFromDb(_db);
                        IsDbLoaded = true;
                    }

                    Status = "Conectado";
                    Log(LogLevel.Ok, $"Conectado a {ComPort}@{BaudRate}");
                    LastRefreshText = DateTime.Now.ToString("HH:mm:ss");
                    NotifyConnectionChanged();
                }
            }
            catch (Exception ex)
            {
                SetError("Excepción: " + ex.Message);
                Log(LogLevel.Error, ex.ToString());
                FailConnection();
            }
            finally
            {
                IsConnecting = false;
                RaiseStateChanged();
            }
        }

        /// <summary>
        /// Lógica pesada de conexión (abrir puerto, init del SDK, handshake).
        /// Se ejecuta en un hilo de fondo; no toca la UI directamente.
        /// </summary>
        private bool DoConnectHeavy()
        {
            // Validaciones previas
            if (!File.Exists(HrlPath)) { SetError("HRL no existe"); return false; }
            if (!File.Exists(GhtPath)) { SetError("GHT no existe"); return false; }
            if (!int.TryParse(BaudRate, out int baud) || baud <= 0) { SetError("Baudios inválidos"); return false; }

            // Abre la BD si aún no se hizo
            if (!IsDbLoaded || _db == null)
            {
                Log(LogLevel.Info, "BD no cargada. Abriendo en hilo de fondo...");
                var db = new Sad.DataBase();
                if (!db.Open(HrlPath, GhtPath)) { SetError("No se pudo abrir la base de datos"); return false; }
                _db = db;
            }

            _sad = new Sad();
            _serial = new Sad.SerialDevice();

            if (!_serial.Open(ComPort, baud))
            {
                SetError("No se pudo abrir el puerto serie");
                FailConnection();
                return false;
            }
            Log(LogLevel.Ok, $"Puerto abierto: {ComPort}@{baud}");

            if (_sad.Init(_db, _serial) != 0)
            {
                SetError("sad.Init falló");
                FailConnection();
                return false;
            }

            // Reintentos de conexión (máx. 3)
            for (int i = 0; i < 3 && !_sad.IsConnected(); i++)
            {
                Log(LogLevel.Info, $"Connect() intento {i + 1}...");
                _sad.Connect(3);
            }

            if (!_sad.IsConnected())
            {
                SetError("Sin conexión con la electrónica");
                FailConnection();
                return false;
            }

            // Configuración de permisos y notificación de cambios de piezas
            _sad.SetPermission(Permission.AllowACK);
            _sad.SetPermission(Permission.AllowAll);
            _sad.ChangesBehavior = Behavior.BasedOnDelegates;
            _sad.AddOnPieceChangeDelegate(OnPieceChanged);

            return true;
        }

        /// <summary>
        /// Cierra la conexión y libera los recursos del SDK.
        /// </summary>
        public void Disconnect()
        {
            try { _sad?.Dispose(); }
            catch (Exception ex) { Log(LogLevel.Error, "Dispose: " + ex.Message); }

            _sad = null;
            _serial = null;

            Status = "Desconectado";
            Log(LogLevel.Info, "Desconectado.");
            NotifyConnectionChanged();
        }

        /// <summary>Llamado al cerrar la app para liberar la conexión.</summary>
        public void Shutdown() => Disconnect();

        /// <summary>Limpia el estado cuando la conexión falla.</summary>
        private void FailConnection()
        {
            try { _sad?.Dispose(); } catch { }
            _sad = null;
            _serial = null;
            NotifyConnectionChanged();
        }

        /// <summary>
        /// Notifica el cambio de estado de conexión a la UI y a todas las piezas.
        /// </summary>
        private void NotifyConnectionChanged()
        {
            OnPropertyChanged(nameof(IsConnected));
            foreach (var b in Blocks)
                b.RefreshConnectionState();
            RaiseStateChanged();
        }

        // ==============================================================
        // CALLBACK NATIVO
        // ==============================================================
        /// <summary>
        /// Callback invocado por el SDK cuando una pieza cambia de valor.
        /// Localiza el <see cref="PieceViewModel"/> correspondiente y lo refresca.
        /// </summary>
        private void OnPieceChanged(Sad.Piece piece)
        {
            if (piece == null) return;

            IntPtr blockHandle = Simpro.Api.get_piece_block(piece.Handle);

            BlockViewModel bvm;
            lock (_blockByHandle) // El índice puede modificarse en otro hilo
            {
                if (!_blockByHandle.TryGetValue(blockHandle, out bvm)) return;
            }

            var pvm = bvm.FindByHandle(piece.Handle);
            if (pvm == null) return;

            // Actualiza la UI en el hilo principal
            InvokeUI(() =>
            {
                pvm.Refresh();
                LastRefreshText = DateTime.Now.ToString("HH:mm:ss");
            });
        }

        // ==============================================================
        // FILTROS
        // ==============================================================
        /// <summary>
        /// Reconstruye <see cref="PiecesView"/> aplicando el filtro de texto
        /// sobre las piezas del bloque seleccionado.
        /// </summary>
        private void ApplyFilter()
        {
            PiecesView.Clear();
            if (SelectedBlock == null) { VisibleCount = 0; return; }

            var q = SelectedBlock.Pieces.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(Filter))
            {
                var f = Filter.Trim();
                q = q.Where(p => p.Name?.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            foreach (var p in q) PiecesView.Add(p);
            VisibleCount = PiecesView.Count;
        }

        /// <summary>
        /// Refresca manualmente todos los valores de todas las piezas.
        /// </summary>
        private void RefreshAllValues()
        {
            foreach (var b in Blocks)
                foreach (var p in b.Pieces)
                    p.Refresh();

            LastRefreshText = DateTime.Now.ToString("HH:mm:ss");
            Log(LogLevel.Info, "Refresco manual completado.");
        }

        // ==============================================================
        // LOG
        // ==============================================================
        /// <summary>
        /// Agrega una línea al log (marshaleando a la UI) y la imprime en Debug.
        /// Mantiene un límite de 2000 entradas para evitar crecimiento indefinido.
        /// </summary>
        private void Log(LogLevel level, string message)
        {
            var line = new LogLine { Time = DateTime.Now, Level = level, Message = message };

            InvokeUI(() =>
            {
                LogLines.Add(line);
                if (level == LogLevel.Error) ErrorCount++;
                // Recorte FIFO para limitar el tamaño del log
                if (LogLines.Count > 2000) LogLines.RemoveAt(0);
                OnPropertyChanged(nameof(FilteredLogLines));
            });

            System.Diagnostics.Debug.WriteLine(line.ToString());
        }

        /// <summary>
        /// Copia al portapapeles el log filtrado actual.
        /// </summary>
        private void CopyLog()
        {
            try
            {
                var text = string.Join(Environment.NewLine, FilteredLogLines.Cast<LogLine>());
                Clipboard.SetText(text);
                Log(LogLevel.Ok, "Log copiado al portapapeles.");
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "CopyLog: " + ex.Message);
            }
        }

        /// <summary>
        /// Marca el estado como error y registra el mensaje.
        /// </summary>
        private void SetError(string message)
        {
            Status = "Error: " + message;
            Log(LogLevel.Error, message);
            RaiseStateChanged();
        }

        // ==============================================================
        // HELPERS
        // ==============================================================
        /// <summary>Fuerza a WPF a reevaluar CanExecute de todos los comandos.</summary>
        private void RaiseStateChanged() => CommandManager.InvalidateRequerySuggested();

        /// <summary>
        /// Ejecuta una acción en el hilo de la UI. Si ya se está en él,
        /// se ejecuta directamente.
        /// </summary>
        private static void InvokeUI(Action action)
        {
            var d = Application.Current?.Dispatcher;
            if (d == null || d.CheckAccess()) action();
            else d.Invoke(action);
        }

        /// <summary>Evento estándar de <see cref="INotifyPropertyChanged"/>.</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Asigna un campo y notifica el cambio si el valor difiere del actual.
        /// </summary>
        /// <returns><c>true</c> si el valor cambió y se notificó; <c>false</c> en caso contrario.</returns>
        protected bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Notifica a la UI que cambió una propiedad.
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // ==============================================================
    // RelayCommand
    // ==============================================================
    /// <summary>
    /// Implementación simple de <see cref="ICommand"/> que delega
    /// la ejecución y el <c>CanExecute</c> a delegados.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object parameter) => _execute(parameter);

        // Se engancha al mecanismo global de WPF para reevaluar CanExecute
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        /// <summary>Solicita a WPF reevaluar <see cref="CanExecute"/>.</summary>
        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }
}