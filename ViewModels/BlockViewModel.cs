using Simpro;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SadViewer.ViewModels
{
    /// <summary>
    /// ViewModel que representa un <see cref="Sad.Block"/> y expone su colección de piezas
    /// (<see cref="PieceViewModel"/>) para el enlace de datos en la vista.
    /// </summary>
    /// <remarks>
    /// Responsabilidades:
    /// - Envolver el modelo de dominio <see cref="Sad.Block"/>.
    /// - Construir los ViewModels de las piezas visibles (excluyendo las de tipo Logic).
    /// - Permitir búsquedas rápidas de piezas por su <see cref="IntPtr"/> (handle).
    /// - Propagar el refresco del estado de conexión a todas sus piezas.
    /// </remarks>
    public class BlockViewModel : INotifyPropertyChanged
    {
        /// <summary>Modelo de dominio subyacente del bloque.</summary>
        public Sad.Block Model { get; }

        /// <summary>
        /// Colección observable con los ViewModels de las piezas visibles del bloque.
        /// Se notifica automáticamente a la UI al agregar/quitar elementos.
        /// </summary>
        public ObservableCollection<PieceViewModel> Pieces { get; } = new();

        /// <summary>
        /// Índice interno para resolver rápidamente un <see cref="PieceViewModel"/>
        /// a partir del handle nativo de la pieza.
        /// </summary>
        private readonly Dictionary<IntPtr, PieceViewModel> _byHandle = new();

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="BlockViewModel"/>.
        /// </summary>
        /// <param name="model">Modelo del bloque a envolver. No puede ser nulo.</param>
        /// <param name="isConnectedProvider">
        /// Delegado que indica si la conexión con el dispositivo/servicio está activa.
        /// Se pasa a cada <see cref="PieceViewModel"/> para que ajusten su comportamiento.
        /// </param>
        /// <param name="logger">Acción para registrar mensajes de log.</param>
        /// <exception cref="ArgumentNullException">
        /// Se lanza si <paramref name="model"/> es nulo.
        /// </exception>
        public BlockViewModel(Sad.Block model, Func<bool> isConnectedProvider, Action<string> logger)
        {
            // Validación defensiva: el modelo es obligatorio para que el VM tenga sentido
            Model = model ?? throw new ArgumentNullException(nameof(model));

            // Se itera sobre todas las piezas del modelo para crear sus ViewModels
            foreach (var p in model.Pieces)
            {
                // Las piezas de tipo "Logic" se omiten intencionalmente:
                // no deben mostrarse en la UI
                if (p.Type == Sad.PieceType.Logic) continue;

                // Se crea el ViewModel de la pieza con sus dependencias
                var pvm = new PieceViewModel(p, isConnectedProvider, logger);

                // Se agrega a la colección observable que verá la vista
                Pieces.Add(pvm);

                // Se indexa por handle para permitir búsquedas O(1) posteriores
                _byHandle[p.Handle] = pvm;
            }
        }

        /// <summary>Nombre del bloque, delegado al modelo.</summary>
        public string Name => Model.Name;

        /// <summary>
        /// Busca el <see cref="PieceViewModel"/> correspondiente al handle indicado.
        /// </summary>
        /// <param name="handle">Handle nativo de la pieza a buscar.</param>
        /// <returns>
        /// El ViewModel de la pieza si existe; de lo contrario, <c>null</c>.
        /// </returns>
        public PieceViewModel? FindByHandle(IntPtr handle)
            => _byHandle.TryGetValue(handle, out var vm) ? vm : null;

        /// <summary>
        /// Fuerza el refresco del estado de conexión en todas las piezas del bloque.
        /// Útil cuando cambia el estado global de conexión y se requiere reflejarlo en la UI.
        /// </summary>
        public void RefreshConnectionState()
        {
            foreach (var p in Pieces)
                p.RefreshConnectionState();
        }

        /// <summary>Evento estándar de <see cref="INotifyPropertyChanged"/>.</summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Notifica a la UI que una propiedad del ViewModel cambió.
        /// </summary>
        /// <param name="p">
        /// Nombre de la propiedad modificada. Se infiere automáticamente
        /// gracias a <see cref="CallerMemberNameAttribute"/>.
        /// </param>
        protected void OnPropertyChanged(string? p)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}