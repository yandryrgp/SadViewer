using Simpro;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SadViewer.ViewModels
{
    /// <summary>
    /// ViewModel que envuelve una pieza individual (<see cref="Sad.Piece"/>) del SDK.
    /// </summary>
    /// <remarks>
    /// Expone las propiedades necesarias para el binding de la vista:
    /// nombre, tipo, valor, valores previo/default, delta y estado de edición.
    /// Solo las piezas de tipo <see cref="Sad.PieceType.Output"/> pueden modificarse,
    /// y únicamente si hay conexión activa con la electrónica.
    /// </remarks>
    public class PieceViewModel : INotifyPropertyChanged
    {
        /// <summary>Modelo de dominio de la pieza.</summary>
        public Sad.Piece? Model { get; }

        /// <summary>Proveedor del estado de conexión (inyectado desde <see cref="BlockViewModel"/>).</summary>
        private readonly Func<bool> _isConnectedProvider;

        /// <summary>Acción para registrar mensajes de log.</summary>
        private readonly Action<string> _logger;

        /// <summary>
        /// Inicializa el ViewModel de una pieza.
        /// </summary>
        /// <param name="model">Modelo de dominio de la pieza.</param>
        /// <param name="isConnectedProvider">
        /// Función que indica si hay conexión activa. Si es <c>null</c>, se asume desconectado.
        /// </param>
        /// <param name="logger">Acción opcional para registrar advertencias.</param>
        public PieceViewModel(Sad.Piece model,
                              Func<bool> isConnectedProvider = null,
                              Action<string> logger = null)
        {
            Model = model;
            _isConnectedProvider = isConnectedProvider ?? (() => false);
            _logger = logger;
        }

        /// <summary>Nombre de la pieza.</summary>
        public string Name => Model.Name;

        /// <summary>Handle nativo de la pieza en formato hexadecimal (útil para depuración).</summary>
        public string HandleHex => "0x" + Model.Handle.ToInt64().ToString("X8");

        /// <summary>Texto legible del tipo de pieza (Input/Output/Timer/Logic/Unknow).</summary>
        public string TypeText
        {
            get
            {
                switch (Model.Type)
                {
                    case Sad.PieceType.Input: return "Input";
                    case Sad.PieceType.Output: return "Output";
                    case Sad.PieceType.Timer: return "Timer";
                    case Sad.PieceType.Logic: return "Logic";
                    default: return "Unknow";
                }
            }
        }

        /// <summary>Indica si la pieza es de tipo salida (solo estas son editables).</summary>
        public bool IsOutputType => Model.Type == Sad.PieceType.Output;

        /// <summary>Indica si hay conexión con la electrónica.</summary>
        public bool IsConnected => _isConnectedProvider();

        /// <summary>Indica si la pieza puede editarse (Output + conectado).</summary>
        public bool IsEditable => IsOutputType && IsConnected;

        /// <summary>Indicador visual (candado) para las no editables.</summary>
        public string LockGlyph => IsEditable ? "\uE785" : "\uE72E"; // unlock / lock

        /// <summary>
        /// Valor actual de la pieza. Solo se permite escribir en piezas Output
        /// y con conexión activa. En caso contrario, se ignora el cambio y se
        /// notifica para que la vista revierta el valor visual.
        /// </summary>
        public int Value
        {
            get => Model.Value;
            set
            {
                // Validación: solo Output
                if (!IsOutputType)
                {
                    _logger?.Invoke($"⚠ {Name}: solo las piezas Output se pueden editar.");
                    OnPropertyChanged(); // Fuerza a la vista a recargar el valor original
                    return;
                }
                // Validación: requiere conexión
                if (!IsConnected)
                {
                    _logger?.Invoke($"⚠ {Name}: no hay conexión con la electrónica.");
                    OnPropertyChanged();
                    return;
                }
                // Sin cambios reales, no se notifica
                if (Model.Value == value) return;

                Model.Value = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PreviousValue));
                OnPropertyChanged(nameof(Delta));
            }
        }

        /// <summary>Valor previo de la pieza (según el SDK).</summary>
        public int? PreviousValue => Model.PreviousValue;

        /// <summary>Valor por defecto de la pieza (según el SDK).</summary>
        public int? DefaultValue => Model.DefaultValue;

        /// <summary>Diferencia entre el valor actual y el anterior.</summary>
        public int? Delta => Model.Value - Model.PreviousValue;

        /// <summary>
        /// Refresca las propiedades dependientes del valor.
        /// Se invoca cuando el SDK notifica un cambio en la pieza.
        /// </summary>
        public void Refresh()
        {
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(PreviousValue));
            OnPropertyChanged(nameof(Delta));
            OnPropertyChanged(nameof(LockGlyph));
        }

        /// <summary>
        /// Refresca las propiedades que dependen del estado de conexión.
        /// </summary>
        public void RefreshConnectionState()
        {
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(IsEditable));
            OnPropertyChanged(nameof(LockGlyph));
        }

        /// <summary>Evento estándar de <see cref="INotifyPropertyChanged"/>.</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notifica a la UI que una propiedad cambió.
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}