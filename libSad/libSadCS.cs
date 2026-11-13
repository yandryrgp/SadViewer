/** 
 * Wrapper for libSad native library v2.7.2
 * 
 * Copyright (c) 1991-2026 SIMPRO
 * By Jordanys Martinez -  Software Department
 */
using Newtonsoft.Json;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using static Simpro.Api;

namespace Simpro
{
    internal static class Import
    {
        public const string lib = "libSad64.dll";

    }


    [StructLayout(LayoutKind.Sequential)]
    public struct PieceChangeEvent
    {
        public IntPtr? PiecePtr;
        public double Value;
        public int blockIndex;
        public int pieceIndex;
    }


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DiscreteState
    {
        // SAD_MAX_STR_LEN debería definirse según el valor original en C
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SAD_MAX_STR_LEN)]
        public string name;

        public double value;
    }

    // Para el puntero, en C# normalmente usarías:
    // IntPtr para interoperabilidad, o simplemente ref/out parameters

    public class Api
    {
        // Usar LoadLibrary/FreeLibrary para mejor control
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        public static extern IntPtr LoadLibrary(string dllName);

        public const int SAD_MAX_STR_LEN = 100;

        [DllImport(Import.lib, EntryPoint = "sad_open_db", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr open_db(IntPtr hrlFile, IntPtr ghtFile);

        [DllImport(Import.lib, EntryPoint = "sad_open_serial_device", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr open_serial_device(IntPtr com, int baudRate, char inByteSize, char iParity, char iCantStopBits);

        [DllImport(Import.lib, EntryPoint = "sad_open_network_device", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr open_network_device(int netProtocol, IntPtr hostIP, short port);

        /* SADManager api */
        [DllImport(Import.lib, EntryPoint = "sad_create_context", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr create_context(IntPtr sadDb, IntPtr ioDevice);

        [DllImport(Import.lib, EntryPoint = "sad_connect", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool connect(IntPtr sadPtr, int waitSeconds);

        [DllImport(Import.lib, EntryPoint = "sad_is_connected", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool isConnected(IntPtr sadPtr);

        [DllImport(Import.lib, EntryPoint = "sad_set_global_permission", CallingConvention = CallingConvention.Cdecl)]
        public static extern void set_global_permission(IntPtr sadPtr, int permision);

        [DllImport(Import.lib, EntryPoint = "sad_set_change_behavior", CallingConvention = CallingConvention.Cdecl)]
        public static extern int set_change_behavior(IntPtr sadPtr, int value);

        [DllImport(Import.lib, EntryPoint = "sad_get_change_behavior", CallingConvention = CallingConvention.Cdecl)]
        public static extern int get_change_behavior(IntPtr sadPtr);

        [DllImport(Import.lib, EntryPoint = "sad_pop_last_piece_change", CallingConvention = CallingConvention.Cdecl)]
        public static extern int pop_last_piece_change(IntPtr sadPtr, ref PieceChangeEvent outPieceChange);


        /**
         * Callbacks interfaces
         */

        public delegate void OnPieceChangeCallback(IntPtr userListener, IntPtr sadPtr, IntPtr pieceHandle, double value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void SetOnPieceChangeDelegate(OnPieceChangeCallback callback);

        [DllImport(Import.lib, EntryPoint = "sad_set_on_piece_change_callback", CallingConvention = CallingConvention.Cdecl)]
        public static extern int set_on_piece_change_callback(IntPtr sadPtr, IntPtr userListener, IntPtr callbackPtr);

        public delegate void OnCmdResponseDelegate(IntPtr userListener, IntPtr sadPtr, int cmd);

        [DllImport(Import.lib, EntryPoint = "sad_set_on_cmd_response_callback", CallingConvention = CallingConvention.Cdecl)]
        public static extern void set_on_cmd_response_callback(IntPtr sadPtr, IntPtr userListener, OnCmdResponseDelegate callbackPtr);

        /**
         * Block and piece
         */
        [DllImport(Import.lib, EntryPoint = "sad_write_piece_value", CallingConvention = CallingConvention.Cdecl)]
        public static extern int write_piece_value(IntPtr blockName, IntPtr pieceName, double value);

        [DllImport(Import.lib, EntryPoint = "sad_write_piece_state_directly", CallingConvention = CallingConvention.Cdecl)]
        public static extern int write_piece_state_directly(IntPtr piecePtr, IntPtr stateName);

        [DllImport(Import.lib, EntryPoint = "sad_write_piece_value_directly", CallingConvention = CallingConvention.Cdecl)]
        public static extern int write_piece_value_directly(IntPtr sadPtr, IntPtr piecePtr, double value);

        [DllImport(Import.lib, EntryPoint = "sad_get_block_name", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr get_block_name(IntPtr blockPtr);

        [DllImport(Import.lib, EntryPoint = "sad_get_block_pieces_count", CallingConvention = CallingConvention.Cdecl)]
        public static extern int get_block_pieces_count(IntPtr blockPtr);

        [DllImport(Import.lib, EntryPoint = "sad_get_blocks_count", CallingConvention = CallingConvention.Cdecl)]
        public static extern int get_blocks_count(IntPtr dbPtr);

        [DllImport(Import.lib, EntryPoint = "sad_get_block_at", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr get_block_at(IntPtr dbPtr, int index);

        [DllImport(Import.lib, EntryPoint = "sad_get_piece_name", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr get_piece_name(IntPtr piecePtr);

        [DllImport(Import.lib, EntryPoint = "sad_get_piece_block", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr get_piece_block(IntPtr piecePtr);

        [DllImport(Import.lib, EntryPoint = "sad_get_piece_default_value", CallingConvention = CallingConvention.Cdecl)]
        public static extern double get_piece_default_value(IntPtr piecePtr);

        [DllImport(Import.lib, EntryPoint = "sad_get_piece_previous_value", CallingConvention = CallingConvention.Cdecl)]
        public static extern double get_piece_previous_value(IntPtr piecePtr);

        [DllImport(Import.lib, EntryPoint = "sad_get_piece_value", CallingConvention = CallingConvention.Cdecl)]
        public static extern double get_piece_value(IntPtr piecePtr);

        [DllImport(Import.lib, EntryPoint = "sad_find_piece_in_block", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr find_piece_in_block(IntPtr db, IntPtr blockPtr, IntPtr pieceName);

        [DllImport(Import.lib, EntryPoint = "sad_get_block_piece_at", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr get_block_piece_at(IntPtr blockPtr, int pieceIndex);

        [DllImport(Import.lib, EntryPoint = "sad_get_piece_states_count", CallingConvention = CallingConvention.Cdecl)]
        public static extern int get_piece_states_count(IntPtr piecePtr);

        [DllImport(Import.lib, EntryPoint = "sad_get_piece_discrete_state", CallingConvention = CallingConvention.Cdecl)]
        public static extern int get_piece_discrete_state(IntPtr piecePtr, int index, ref DiscreteState state);

        [DllImport(Import.lib, EntryPoint = "sad_get_piece_type", CallingConvention = CallingConvention.Cdecl)]
        public static extern int get_piece_type(IntPtr piecePtr);

        /**         
         * Change Streaming
         */
        [DllImport(Import.lib, EntryPoint = "sad_enable_tcp_change_streaming", CallingConvention = CallingConvention.Cdecl)]
        public static extern int enable_tcp_change_streaming(IntPtr sadPtr, bool value);

        [DllImport(Import.lib, EntryPoint = "sad_is_tcp_change_streaming_enabled", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool is_tcp_change_streaming_enabled(IntPtr sadPtr);
        /**
         * Global
         */
        [DllImport(Import.lib, EntryPoint = "sad_disconnect", CallingConvention = CallingConvention.Cdecl)]
        public static extern void disconnect(IntPtr sadPtr);

        [DllImport(Import.lib, EntryPoint = "sad_kill", CallingConvention = CallingConvention.Cdecl)]
        public static extern void kill(IntPtr sadPtr);

        [DllImport(Import.lib, EntryPoint = "sad_enable_debug_file", CallingConvention = CallingConvention.Cdecl)]
        public static extern void enable_debug_file(bool value);
    }
    /// <summary>
    /// Clase que agrupa las funciones principales para la comunicación directa con el SADManager
    /// </summary>
    public class Sad : IDisposable
    {
        IntPtr handle;

        #region Enums
        public enum Permission
        {
            AllowAll = 19,
            DenyAll = 20,
            AllowACK = 202,
            DenyACK = 203
        }

        public enum Behavior
        {
            BasedOnDelegates = 0,
            BasedOnChangesPool = 1,
            Nothing = 2,
        }

        public enum PieceType
        {
            Input = 0,
            Output = 1,
            Timer = 2,
            Logic = 3,
            Unknow = 65536,
        }
        #endregion


        #region Models
        /// <summary>
        /// Clase base para la gestión del manejador nativo de un componente
        /// </summary>
        public class Component
        {
            protected IntPtr handle;
            /// <summary>
            /// Función para validadar si el manejador es válido.
            /// </summary>
            public bool IsValid
            {
                get
                {
                    return handle != IntPtr.Zero;
                }
            }
            /// <summary>
            /// Función que devuelve el manejador nativo del componente
            /// </summary>
            public IntPtr Handle
            {
                get
                {
                    return handle;
                }
            }
        }

        class Cmd : IDisposable
        {
            public int? Value { get; set; }

            public void Dispose()
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Clase que encapsula las funcionalidades de una pieza correspondiente a un bloque
        /// </summary>
        public class Piece : Component, IDisposable
        {


            public IntPtr SadPtr { get; set; }
            public class State
            {
                public State(IntPtr pieceHandle, DiscreteState discreteState)
                {
                    this.pieceHandle = pieceHandle;
                    ds = discreteState;
                }

                public string? Name
                {
                    get
                    {
                        return ds.name;
                    }
                }

                public double? Value
                {
                    get
                    {
                        return ds.value;
                    }
                }

                DiscreteState ds;
                private protected IntPtr? pieceHandle;
            }
            /// <summary>
            /// Función estática para crear un objeto Pieza a partir de un manejador físico asociado a una pieza.
            /// </summary>
            /// <param name="pieceHandle">Manejador físico de la pieza</param>
            /// <returns></returns>
            public static Piece FromHandle(IntPtr pieceHandle)
            {
                Piece? piece = new Piece();
                piece.handle = pieceHandle;

                int n = get_piece_states_count(pieceHandle);

                for (int i = 0; i < n; i++)
                {
                    DiscreteState discreteState = new DiscreteState();
                    get_piece_discrete_state(pieceHandle, i, ref discreteState);
                    piece.states.Add(new State(pieceHandle, discreteState));
                }


                return piece;
            }


            public List<State>? States
            {
                get
                {
                    return states;
                }
            }

            /// <summary>
            /// Función para destruir una pieza
            /// </summary>
            public void Dispose()
            {
                handle = IntPtr.Zero;
            }
            /// <summary>
            /// Devuelve una cadena de caracteres asociado al nombre físico de una pieza
            /// </summary>
            public string? Name
            {
                get
                {
                    return Marshal.PtrToStringAnsi(Api.get_piece_name(Handle));
                }
            }
            /// <summary>
            /// Función para devolver el valor anterior al último cambio de la pieza
            /// </summary>
            public int? PreviousValue
            {
                get
                {
                    return (int)Api.get_piece_previous_value(Handle);
                }
            }
            /// <summary>
            /// Función para devolver o establecer un valor para la pieza
            /// </summary>
            public int Value
            {
                get
                {
                    if (Handle == IntPtr.Zero)
                        return int.MinValue;

                    return (int)Api.get_piece_value(Handle);
                }
                set
                {
                    if (Handle != IntPtr.Zero)
                    {
                        Api.write_piece_value_directly(SadPtr, Handle, (double)value);
                    }
                }
            }
            /// <summary>
            /// Función para devolver el valor por defecto de la pieza
            /// </summary>
            public int? DefaultValue
            {
                get
                {
                    return (int)Api.get_piece_default_value(Handle);
                }
            }
            /// <summary>
            /// Función para devolver el bloque al que pertenece la pieza
            /// </summary>
            //public Block Block
            //{
            //    get
            //    {
            //        return Block.FromHandle(Api.get_piece_block(handle));
            //    }
            //}

            public bool SetState(string name)
            {
                IntPtr str = Marshal.StringToHGlobalAnsi(name);
                int res = Api.write_piece_state_directly(Handle, str);
                return res == 0;
            }

            public PieceType? Type
            {
                get
                {
                    if (Handle == IntPtr.Zero)
                        return PieceType.Unknow;

                    return (PieceType)get_piece_type(Handle);
                }
            }

            public Type? MyProperty { get; set; }

            private List<State> states = new List<State>();
        }

        /// <summary>
        /// Clase que encapsula las funcionalidades de un bloque físico del SADManager
        /// </summary>
        public class Block : Component
        {
            public Block(IntPtr handle)
            {
                this.handle = handle;

                int n = get_block_pieces_count(handle);

                for (int i = 0; i < n; i++)
                {
                    IntPtr pieceHandle = get_block_piece_at(handle, i);
                    Piece? piece = Piece.FromHandle(pieceHandle);
                    pieces.Add(piece);
                }
            }
            /// <summary>
            /// Devuelve una cadena de caracteres asociado al nombre físico del bloque
            /// </summary>
            public string? Name
            {
                get
                {
                    return Marshal.PtrToStringAnsi(Api.get_block_name(Handle));
                }
            }

            public List<Piece> Pieces
            {

                get
                {
                    return pieces;
                }
            }

            public Piece? Find(string name)
            {
                for (int i = 0; i < pieces.Count; i++)
                {
                    Piece? piece = pieces[i];
                    if (piece.Name == name)
                        return piece;
                }

                return null;
            }

            private List<Piece> pieces = new List<Piece>();

        }

        /// <summary>
        /// Clase base para la definición de un dispositivo de Entrada/Salida del SADManager
        /// </summary>
        public class IODevice : Component
        {
            public const int Zero = 0;
            /// <summary>
            /// Tipos de dispositivos
            /// </summary>
            public enum Type { Serial, Tcp, Udp, Custom = 0xFDE8 };

            /// <summary>
            /// Tipos de protocolos de red
            /// </summary>
            public enum Net { Tcp, Udp };

            /// <summary>
            /// Constructor por defecto
            /// </summary>
            /// <param name="type"></param>
            public IODevice(Type type)
            {
                this.type = type;
            }
            /// <summary>
            /// Devuelve el tipo de disposivo
            /// </summary>
            public Type? DeviceType
            {
                get
                {
                    return type;
                }
            }

            protected Type? type;
        }
        /// <summary>
        /// Clase para la lectura de los archivos ´.HRL´ y ´.GHT´
        /// </summary>
        public class DataBase : Component
        {
            public IntPtr? SadPtr { get; set; }
            /// <summary>
            /// Función para abrir la base de datos a partir de los archivos ´.HRL´ y ´.GHT´
            /// </summary>
            /// <param name="hrlFile">Dirección del archivo ´.HRL´</param>
            /// <param name="ghtFile">Dirección del archivo ´.GHT´</param>
            /// <returns>Devuelve verdadero si se abrieron satisfactoriamente los archivos</returns>
            public bool Open(string hrlFile, string ghtFile)
            {
                handle = Api.open_db(Marshal.StringToHGlobalAnsi(hrlFile), Marshal.StringToHGlobalAnsi(ghtFile));
                int nBlocks = Api.get_blocks_count(Handle);

                for (int i = 0; i < nBlocks; i++)
                {
                    IntPtr blockHandle = get_block_at(handle, i);
                    blocks.Add(new Block(blockHandle));
                }

                return IsValid;
            }

            public Block? FindBlock(string name)
            {
                for (int i = 0; i < blocks.Count; i++)
                {
                    Block? item = blocks[i];
                    if (item.Name == name)
                        return item;
                }

                return null;
            }

            public List<Block> Blocks
            {

                get
                {
                    return blocks;
                }
            }

            List<Block> blocks = new List<Block>();
        }

        /// <summary>
        /// Pool thread-safe para manejar cambios de cualquier tipo de objeto
        /// </summary>
        /// <typeparam name="T">Tipo de objeto a manejar en el pool</typeparam>
        public class SafePool<T> where T : class
        {
            /// <summary>
            /// Función para saber si hay cambios disponibles
            /// </summary>
            /// <returns>Devuelve verdadero si hay cambios. Falso si ocurre lo contrario</returns>
            public bool? Available
            {
                get
                {
                    lock (changesQueue)
                    {
                        return changesQueue.Count > 0;
                    }
                }
            }

            /// <summary>
            /// Cantidad de elementos en el pool
            /// </summary>
            public int? Count
            {
                get
                {
                    lock (changesQueue)
                    {
                        return changesQueue.Count;
                    }
                }
            }

            /// <summary>
            /// Función para extraer el próximo elemento disponible
            /// </summary>
            /// <returns>El elemento o null si no hay elementos disponibles</returns>
            public T? Pop()
            {
                lock (changesQueue)
                {
                    if (changesQueue.Count > 0)
                    {
                        return changesQueue.Dequeue();
                    }
                }

                return null;
            }

            /// <summary>
            /// Función para intentar extraer el próximo elemento disponible
            /// </summary>
            /// <param name="item">El elemento extraído</param>
            /// <returns>True si se extrajo un elemento, false si no había elementos</returns>
            public bool TryPop(out T? item)
            {
                item = Pop();
                return item != null;
            }

            /// <summary>
            /// Función para agregar un elemento al pool
            /// </summary>
            /// <param name="item">Elemento a agregar</param>
            public void Push(T item)
            {
                if (item == null) return;

                lock (changesQueue)
                {
                    changesQueue.Enqueue(item);
                }
            }

            /// <summary>
            /// Función para agregar múltiples elementos al pool
            /// </summary>
            /// <param name="items">Colección de elementos a agregar</param>
            public void PushRange(IEnumerable<T> items)
            {
                if (items == null) return;

                lock (changesQueue)
                {
                    foreach (var item in items)
                    {
                        if (item != null)
                        {
                            changesQueue.Enqueue(item);
                        }
                    }
                }
            }

            /// <summary>
            /// Extrae todos los elementos disponibles del pool
            /// </summary>
            /// <returns>Array con todos los elementos disponibles</returns>
            public T[] PopAll()
            {
                lock (changesQueue)
                {
                    if (changesQueue.Count == 0)
                    {
                        return System.Array.Empty<T>();
                    }

                    var items = changesQueue.ToArray();
                    changesQueue.Clear();
                    return items;
                }
            }

            /// <summary>
            /// Limpia todos los elementos del pool
            /// </summary>
            public void Clear()
            {
                lock (changesQueue)
                {
                    changesQueue.Clear();
                }
            }

            /// <summary>
            /// Obtiene el próximo elemento sin extraerlo (peek)
            /// </summary>
            /// <returns>El próximo elemento o null si no hay elementos</returns>
            public T? Peek()
            {
                lock (changesQueue)
                {
                    if (changesQueue.Count > 0)
                    {
                        return changesQueue.Peek();
                    }
                }

                return null;
            }

            private readonly Queue<T> changesQueue = new Queue<T>();
        }

        /// <summary>
        /// Clase para la comunicación serie con el SADManager
        /// </summary>
        public class TcpDevice : IODevice
        {
            public TcpDevice() : base(Type.Tcp)
            {
            }

            /// <summary>
            /// Función para abrir la conexión TCP
            /// </summary>
            /// <param name="ip">Direccion ip4</param>
            /// <param name="port">Puerto</param>
            /// <returns></returns>
            public virtual bool Connect(string ip, short port)
            {
                handle = Api.open_network_device((int)Net.Tcp, Marshal.StringToHGlobalAnsi(ip), port);
                return IsValid;
            }
        }

        /// <summary>
        /// Clase para la comunicación serie con el SADManager
        /// </summary>
        public class SerialDevice : IODevice
        {
            public const int ByteSize = 8;

            public SerialDevice() : base(Type.Serial)
            {

            }
            /// <summary>
            /// Función para abrir la conexión con el puerto serie
            /// </summary>
            /// <param name="COMPort">Cadenda de caracteres del puerto COM</param>
            /// <param name="baudRate">Velocidad de conexión en baudios</param>
            /// <returns></returns>
            public virtual bool Open(string COMPort, int baudRate)
            {
                handle = Api.open_serial_device(Marshal.StringToHGlobalAnsi(COMPort), baudRate, (char)ByteSize, (char)Zero, (char)Zero);
                return IsValid;
            }
            /// <summary>
            /// Función parametrizada para abrir la conexión con el puerto serie
            /// </summary>
            /// <param name="COMPort">Cadenda de caracteres del puerto COM</param>
            /// <param name="baudRate">Velocidad de conexión en baudios</param>
            /// <param name="inByteSize">Tamaño del byte. Por defecto 8 bits</param>
            /// <param name="iParity">Valor de paridad</param>
            /// <param name="iCantStopBits">Bandera para detener los bits</param>
            /// <returns></returns>
            public virtual bool Open(string COMPort, int baudRate, char inByteSize, char iParity, char iCantStopBits)
            {
                handle = Api.open_serial_device(Marshal.StringToHGlobalAnsi(COMPort), baudRate, inByteSize, iParity, iCantStopBits);
                return IsValid;
            }
        }

        /// <summary>
        /// Cliente TCP para conectarse al servicio LibSad en el puerto 8000.
        /// Recibe notificaciones en tiempo real de cambios de valor en piezas del SADManager.
        /// </summary>
        public class StreamingClient : IDisposable
        {
            #region Fields
            private readonly string _host = "";
            private readonly int _port = 0;
            private TcpClient? _client;
            private NetworkStream _stream;
            private CancellationTokenSource? _cts;
            private Task? _listenTask;

            /// <summary>
            /// Evento disparado cuando se recibe un mensaje del servidor.
            /// </summary>
            public event Action<LibSadMessage> OnMessageReceived;

            /// <summary>
            /// Evento disparado cuando ocurre un error.
            /// </summary>
            public event Action<Exception> OnError;

            /// <summary>
            /// Evento disparado cuando la conexión se cierra.
            /// </summary>
            public event Action? OnConnectionClosed;

            /// <summary>
            /// Indica si el cliente está conectado.
            /// </summary>
            public bool IsConnected => _client?.Connected ?? false;
            #endregion

            #region Constructor
            /// <summary>
            /// Constructor del cliente LibSad.
            /// </summary>
            /// <param name="host">Dirección del servidor (por defecto: 127.0.0.1)</param>
            /// <param name="port">Puerto del servidor (por defecto: 8000)</param>
            public StreamingClient(string host = "127.0.0.1", int port = 8000)
            {
                _host = host ?? throw new ArgumentNullException(nameof(host));
                _port = port;
            }
            #endregion

            #region Public Methods
            /// <summary>
            /// Conecta al servidor LibSad de forma asíncrona.
            /// </summary>
            /// <returns>True si la conexión fue exitosa, False en caso contrario.</returns>
            public async Task<bool> ConnectAsync()
            {
                try
                {
                    _client = new TcpClient();
                    await _client.ConnectAsync(_host, _port);
                    _stream = _client.GetStream();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            /// <summary>
            /// Inicia la escucha de mensajes en un hilo separado.
            /// </summary>
            public void Start()
            {
                if (!IsConnected)
                    throw new InvalidOperationException("Must connect first using ConnectAsync()");

                _cts = new CancellationTokenSource();
                _listenTask = Task.Run(() => ListenLoop(_cts.Token));
            }

            /// <summary>
            /// Detiene la escucha de mensajes.
            /// </summary>
            public void Stop()
            {
                _cts?.Cancel();
                _listenTask?.Wait(TimeSpan.FromSeconds(2));
            }

            /// <summary>
            /// Libera los recursos del cliente.
            /// </summary>
            public void Dispose()
            {
                Stop();
                _stream?.Dispose();
                _client?.Dispose();
                _cts?.Dispose();
            }
            #endregion

            #region Private Methods
            /// <summary>
            /// Bucle principal de escucha para mensajes entrantes.
            /// </summary>
            private async Task ListenLoop(CancellationToken token)
            {
                byte[] buffer = new byte[4096];
                StringBuilder? messageBuffer = new StringBuilder();

                try
                {
                    while (!token.IsCancellationRequested && IsConnected)
                    {
                        try
                        {
                            if (!_stream.DataAvailable)
                            {
                                await Task.Delay(100, token);
                                continue;
                            }

                            int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, token);

                            if (bytesRead == 0)
                            {
                                OnConnectionClosed?.Invoke();
                                break;
                            }

                            string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            messageBuffer.Append(data);

                            ProcessBuffer(messageBuffer);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (IOException ex)
                        {
                            OnError?.Invoke(ex);
                            break;
                        }
                    }

                    Start();
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex);
                }
            }

            /// <summary>
            /// Procesa el búfer de mensajes para extraer mensajes completos.
            /// </summary>
            private void ProcessBuffer(StringBuilder messageBuffer)
            {
                while (true)
                {
                    string bufferContent = messageBuffer.ToString();
                    int contentIndex = bufferContent.IndexOf("content :");

                    if (contentIndex < 0)
                        break; // No hay mensaje completo aún

                    // Buscar inicio del JSON
                    int jsonStart = bufferContent.IndexOf("{", contentIndex);
                    if (jsonStart < 0)
                        break; // No hay JSON completo aún

                    int braceCount = 0;
                    int jsonEnd = -1;

                    // Recorrer el búfer para encontrar la llave de cierre del JSON
                    for (int i = jsonStart; i < bufferContent.Length; i++)
                    {
                        if (bufferContent[i] == '{') braceCount++;
                        if (bufferContent[i] == '}') braceCount--;

                        if (braceCount == 0)
                        {
                            jsonEnd = i + 1; // Posición del último carácter del JSON
                            break;
                        }
                    }

                    if (jsonEnd < 0)
                        break; // JSON incompleto

                    // Extraer mensaje completo (incluyendo encabezados y JSON)
                    string message = bufferContent.Substring(0, jsonEnd);
                    // Eliminar del búfer
                    messageBuffer.Remove(0, jsonEnd);

                    // Procesar mensaje individual
                    ProcessMessage(message);
                }
            }

            /// <summary>
            /// Procesa un mensaje individual.
            /// </summary>
            private void ProcessMessage(string rawMessage)
            {
                try
                {
                    var parsed = ParseMessage(rawMessage);
                    if (parsed != null)
                    {
                        OnMessageReceived?.Invoke(parsed);
                    }
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex);
                }
            }

            /// <summary>
            /// Analiza la cadena de mensaje sin procesar en un objeto LibSadMessage.
            /// </summary>
            private LibSadMessage ParseMessage(string rawMessage)
            {
                var lines = rawMessage.Split('\n');
                if (lines.Length < 3) return null;

                var message = new LibSadMessage();
                string jsonContent = null;

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine)) continue;

                    if (trimmedLine.Contains(":"))
                    {
                        var parts = trimmedLine.Split(new[] { ':' }, 2);
                        string key = parts[0].Trim().ToLower();
                        string value = parts[1].Trim();

                        if (key == "libsad-version" || key == "libsad")
                        {
                            message.Version = value;
                        }
                        else if (key == "content-type")
                        {
                            message.ContentType = value;
                        }
                        else if (key == "content")
                        {
                            jsonContent = value;
                        }
                    }
                }


                if (!string.IsNullOrEmpty(jsonContent) && message.ContentType == "app/json")
                {
                    try
                    {
                        message.Content = JsonConvert.DeserializeObject<ContentData>(jsonContent);
                    }
                    catch (JsonException ex)
                    {
                        OnError?.Invoke(ex);
                    }


                }

                return message;
            }
            #endregion
        }

        /// <summary>
        /// Mensaje completo recibido del servicio TCP-Streaming de LibSad.
        /// Formato: libsad-version, content-type, content
        /// </summary>
        public class LibSadMessage
        {
            /// <summary>
            /// Versión del protocolo LibSad (ej.: 2.7.2)
            /// </summary>
            public string? Version { get; set; }

            /// <summary>
            /// Tipo de contenido del mensaje (ej.: app/json)
            /// </summary>
            public string? ContentType { get; set; }

            /// <summary>
            /// Datos de contenido analizados.
            /// </summary>
            public ContentData? Content { get; set; }
        }

        /// <summary>
        /// Contenedor de datos para el mensaje de LibSad.
        /// </summary>
        public class ContentData
        {
            /// <summary>
            /// Información de la pieza que cambió su valor.
            /// </summary>
            public PieceStreaming? piece { get; set; }
        }

        public class PieceStreaming
        {
            /// <summary>
            /// Nombre de la pieza (ej: HEART_BEAT, SENSOR_01, etc.)
            /// </summary>
            public string? name { get; set; }

            /// <summary>
            /// Bloque al que pertenece la pieza (ej: SADManager, CONTROL, etc.)
            /// </summary>
            public string? block { get; set; }

            /// <summary>
            /// Valor actual de la pieza
            /// </summary>
            public int? value { get; set; }

            /// <summary>
            /// Valor anterior de la pieza antes del cambio
            /// </summary>
            public int? previousValue { get; set; }

            /// <summary>
            /// Valor del tipo de pieza
            /// </summary>
            public int? type { get; set; }
        }
        #endregion

        /// <summary>
        /// Función delegada que debe implementarse para recibir los cambios asociados a una pieza en el SADManager.
        /// </summary>
        /// <param name="piece"></param>
        public delegate void OnPieceChangeDelegate(Piece piece);

        /// <summary>
        /// Función delegada que debe implementarse para recibir el manejador de ejecucion de comandos.
        /// </summary>
        /// <param name="piece"></param>
        public delegate void OnCmdResponseDelegate(int cmd);

        protected void OnCmdResponseCallback(IntPtr sadPtr, IntPtr listener, int cmd)
        {
            foreach (var item in onCmdResponseDelegates)
            {
                item.Invoke(cmd);
            }
        }

        /// <summary>
        /// Función para establecer la configuración general del SADManager
        /// </summary>
        /// <param name="dataBase">Objeto a una Base de datos</param>
        /// <param name="ioDevice">Objeto de un dispositivo de Entrada/Salida</param>
        /// <returns></returns>
        public int Init(DataBase dataBase, IODevice ioDevice)
        {
            handle = Api.create_context(dataBase.Handle, ioDevice.Handle);
            db = dataBase;
            db.SadPtr = handle;

            for (int i = 0; i < db.Blocks.Count; i++)
            {
                Block? block = db.Blocks[i];
                for (int j = 0; j < db.Blocks[i].Pieces.Count; j++)
                {
                    Piece? piece = block.Pieces[j];
                    piece.SadPtr = handle;
                }
            }


            onPieceChangeCallback = new OnPieceChangeCallback(OnPieceChange);
            _callbackHandle = GCHandle.Alloc(onPieceChangeCallback);
            _callbackPtr = Marshal.GetFunctionPointerForDelegate(onPieceChangeCallback);

            // Pasar this como userListener para recuperar la instancia
            _selfHandle = GCHandle.Alloc(this, GCHandleType.Weak);
            IntPtr thisPtr = GCHandle.ToIntPtr(_selfHandle);

            set_on_piece_change_callback(handle, thisPtr, _callbackPtr);

            return 0;
        }

        private void DoOnPieceChange(IntPtr pieceHandle)
        {
            IntPtr? blockPtr = get_piece_block(pieceHandle);

            foreach (var block in db.Blocks)
            {
                if (block.Handle == blockPtr)
                {
                    foreach (var piece in block.Pieces)
                    {
                        if (piece.Handle == pieceHandle)
                        {
                            foreach (var item in onPieceChangeDelegates)
                            {
                                try
                                {

                                    item.Invoke(piece);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                    throw;
                                }

                            }
                            break;
                        }
                    }
                    break;
                }
            }
        }

        private static void OnPieceChange(IntPtr userListener, IntPtr sadPtr, IntPtr pieceHandle, double value)
        {
            try
            {
                GCHandle handle = GCHandle.FromIntPtr(userListener);
                if (handle.Target is Sad sadInstance)
                {
                    sadInstance.DoOnPieceChange(pieceHandle);
                }
            }
            catch
            {
                // Silenciar excepciones para no romper el código nativo
            }
        }

        /// <summary>
        /// Función para establecer el manejador de respuesta a la ejecucion de un comando
        /// </summary>
        /// <param name="OnCmdResponseDelegate">Función delegada para recibir el comando que fue ejecutado en el SADManager.</param>
        /// <returns></returns>
        public void SetOnCmdResponseDelegate(OnCmdResponseDelegate value)
        {
            Api.set_on_cmd_response_callback(handle, IntPtr.Zero, OnCmdResponseCallback);

            if (value != null)
                onCmdResponseDelegates.Add(value);
        }

        /// <summary>
        /// Función para establecer la conexión hacia el SADManager.
        /// </summary>
        /// <param name="waitSeconds">Segundos de espera para sincrónica para establecer la conexión. Si el valor es cero el tipo de conexión es asíncrona y el mensaje de respuesta de conexión llega en otro momento</param>
        /// <returns></returns>
        public bool Connect(int waitSeconds)
        {
            return Api.connect(handle, waitSeconds);
        }

        /// <summary>
        /// Función para detrminar si hay conexion con el SADManager.
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            return Api.isConnected(handle);
        }

        /// <summary>
        /// Función para establecer un permiso al SADManager.
        /// </summary>
        /// <param name="permission">Identificador del permiso</param>
        public void SetPermission(Permission permission)
        {
            Api.set_global_permission(handle, (int)permission);
        }

        /// <summary>
        /// Función nativa para escribir un cambio en una pieza
        /// </summary>
        /// <param name="blockName">Cadena de caracteres asociada al nombre del bloque</param>
        /// <param name="pieceName">Cadena de caracteres asociada al nombre de la pieza</param>
        /// <param name="value">Valor a establecer</param>
        /// <returns></returns>
        public int Write(string blockName, string pieceName, double value)
        {
            return Api.write_piece_value(Marshal.StringToHGlobalAnsi(blockName), Marshal.StringToHGlobalAnsi(pieceName), value);
        }

        /// <summary>
        /// Función para resetear el SADManager
        /// </summary>
        public void Kill()
        {
            Api.kill(handle);
        }

        /// <summary>
        /// Función para establecer el comportamiento de procesamiento de los cambios en el SADManager.
        /// </summary>
        /// <param name="behavior"></param>
        public Behavior ChangesBehavior
        {
            set
            {
                behavior = (int)value;
                if (handle != IntPtr.Zero)
                    Api.set_change_behavior(handle, (int)value);
            }

            get
            {
                return (Behavior)Api.get_change_behavior(handle);
            }


        }

        /// <summary>
        /// Habilitar o no el archivo ´Debug.log´
        /// </summary>
        /// <param name="value">Valor boolean</param>
        void EnableDebugFile(bool value)
        {
            Api.enable_debug_file(value);
        }

        /// <summary>
        /// Agrega un delegado para manejar un cambio de valor de una pieza
        /// </summary>
        /// <param name="value">Objeto delegado</param>
        public void AddOnPieceChangeDelegate(OnPieceChangeDelegate value)
        {
            onPieceChangeDelegates.Add(value);
        }

        PieceChangeEvent change = new PieceChangeEvent();

        /// <summary>
        /// Obtener la ultima pieza q cambio
        /// </summary>
        /// <returns></returns>
        public Piece PopLastChange()
        {
            // Validar estado
            if (handle == IntPtr.Zero || db == null || db.Handle == IntPtr.Zero)
                return null;


            int nres = Api.pop_last_piece_change(handle, ref change);

            // nres == 0 => éxito (según uso actual en el proyecto)
            if (nres == 0 && change.PiecePtr != IntPtr.Zero)
            {
                return db.Blocks[change.blockIndex].Pieces[change.pieceIndex];
                //return null;// Piece.FromHandle(handle, db.Handle, change.PiecePtr);
            }

            // Opcional: registrar nres para diagnóstico
            // Debug.WriteLine($"pop_last_piece_change returned {nres}");

            return null;
        }

        /// <summary>
        /// Varaible de inicio de Streaming
        /// </summary>
        public bool TcpStreaming
        {
            get
            {
                if (handle != IntPtr.Zero)
                    return Api.is_tcp_change_streaming_enabled(handle);

                return false;
            }
            set
            {
                if (handle != IntPtr.Zero)
                    Api.enable_tcp_change_streaming(handle, value);
            }
        }

        public StreamingClient streamingClient;

        /// <summary>
        /// Funcion para cerrar sad
        /// </summary>
        public void Dispose()
        {
            if (_callbackHandle.IsAllocated)
            {
                _callbackHandle.Free();
                _callbackPtr = IntPtr.Zero;
                onPieceChangeCallback = null;

                Api.disconnect(handle);
            }
        }


        private List<OnCmdResponseDelegate> onCmdResponseDelegates = new List<OnCmdResponseDelegate>();
        private List<OnPieceChangeDelegate> onPieceChangeDelegates = new List<OnPieceChangeDelegate>();

        private DataBase db = null;
        private IODevice device = null;
        private int behavior = 0;

        private IntPtr _callbackPtr;
        private OnPieceChangeCallback? onPieceChangeCallback = null;
        private static GCHandle _callbackHandle;
        private GCHandle _selfHandle;
    }
}
