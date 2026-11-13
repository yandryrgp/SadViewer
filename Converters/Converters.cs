using SadViewer.ViewModels;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SadViewer.Converters
{
    /// <summary>
    /// Convierte un valor de tipo <see cref="LogLevel"/> en un <see cref="SolidColorBrush"/>
    /// con el color asociado a cada nivel de log.
    /// </summary>
    /// <remarks>
    /// Mapeo de colores:
    /// - Ok    → verde   (#3BA55D)
    /// - Warn  → amarillo(#F0B232)
    /// - Error → rojo    (#ED4245)
    /// - Otro  → gris    (#9AA0A6)
    /// Si el valor no es un <see cref="LogLevel"/>, se devuelve gris por defecto.
    /// </remarks>
    public class LevelToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Solo se procesa si el valor recibido es un LogLevel válido
            if (value is LogLevel lvl)
            {
                switch (lvl)
                {
                    case LogLevel.Ok: return new SolidColorBrush(Color.FromRgb(0x3B, 0xA5, 0x5D)); // Verde: éxito
                    case LogLevel.Warn: return new SolidColorBrush(Color.FromRgb(0xF0, 0xB2, 0x32)); // Amarillo: advertencia
                    case LogLevel.Error: return new SolidColorBrush(Color.FromRgb(0xED, 0x42, 0x45)); // Rojo: error
                    default: return new SolidColorBrush(Color.FromRgb(0x9A, 0xA0, 0xA6)); // Gris: desconocido
                }
            }
            // Fallback si el valor no es un LogLevel
            return Brushes.Gray;
        }

        // La conversión inversa (Brush → LogLevel) no tiene sentido en este contexto
        public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
    }

    /// <summary>
    /// Convierte un <see cref="bool"/> en un <see cref="Visibility"/>.
    /// Útil para mostrar/ocultar controles enlazados a propiedades booleanas.
    /// </summary>
    /// <remarks>
    /// - true  → Visible
    /// - false → Collapsed
    /// </remarks>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
            => (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type t, object p, CultureInfo c)
            => (value is Visibility v && v == Visibility.Visible);
    }

    /// <summary>
    /// Invierte un valor booleano.
    /// </summary>
    /// <remarks>
    /// Útil para enlazar propiedades donde se necesita el valor contrario
    /// (por ejemplo, habilitar un botón cuando otro estado es falso).
    /// </remarks>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
            => !(value is bool b && b); // Devuelve true si no es bool o si es false

        public object ConvertBack(object value, Type t, object p, CultureInfo c)
            => !(value is bool b && b); // Misma lógica en sentido inverso
    }

    /// <summary>
    /// Convierte un valor en <see cref="Visibility"/> dependiendo de si es <c>null</c> o no.
    /// </summary>
    /// <remarks>
    /// - null      → Collapsed
    /// - no null   → Visible
    /// Útil para mostrar elementos solo cuando hay datos disponibles.
    /// </remarks>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
            => value == null ? Visibility.Collapsed : Visibility.Visible;

        // No se soporta la conversión inversa porque Visibility no determina el valor original
        public object ConvertBack(object value, Type t, object p, CultureInfo c)
            => throw new NotSupportedException();
    }
}