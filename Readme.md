# SAD Viewer

Aplicación de escritorio para Windows desarrollada con **C# y WPF (.NET 10)**, diseñada para supervisar, visualizar y consultar en tiempo real los datos de un sistema SAD mediante la biblioteca nativa `libSad64.dll` y el SDK `Simpro.Sad`.

## Características principales

- **Conexión con el sistema SAD**
  - Selección del puerto serie (COM).
  - Configuración de la velocidad de comunicación.
  - Detección y actualización de los puertos COM disponibles.
  - Inicialización del SDK y establecimiento de la conexión con la electrónica.
  - Hasta tres intentos de conexión.

- **Gestión de bases de datos**
  - Carga de archivos `.hrl` y `.ght`.
  - Selección de archivos desde el explorador de Windows.
  - Lectura de bloques y piezas de la base de datos SAD.
  - Configuración persistente de las rutas de los archivos.

- **Visualización de datos**
  - Listado de bloques y piezas.
  - Selección de bloques.
  - Visualización de las piezas asociadas al bloque seleccionado.
  - Filtrado de piezas por nombre.
  - Actualización de valores mediante eventos del SDK.
  - Actualización manual de los valores.

- **Registro y diagnóstico**
  - Registro de eventos con niveles `Info`, `Ok`, `Warn` y `Error`.
  - Filtros por nivel y contenido del mensaje.
  - Indicador visual del estado de conexión.
  - Contador de errores.
  - Copia del registro al portapapeles.
  - Límite de 2000 entradas para controlar el consumo de memoria.

- **Configuración**
  - Lectura y escritura de `SADConfig.json`.
  - Configuración del puerto, baudios y archivos de base de datos.
  - Valores predeterminados cuando no existe un archivo de configuración válido.

- **Exportación**
  - Generación de códigos de piezas mediante `PieceCodeGenerator`.

## Tecnologías utilizadas

| Tecnología | Uso |
|---|---|
| C# | Lenguaje principal |
| .NET 10 | Plataforma de ejecución |
| WPF | Interfaz gráfica de escritorio |
| XAML | Definición de interfaces |
| Simpro.Sad | Integración con el sistema SAD |
| libSad64.dll | Biblioteca nativa del sistema SAD |
| System.IO.Ports | Comunicación mediante puertos serie |
| System.Text.Json | Lectura y escritura de configuración |
| Newtonsoft.Json | Serialización JSON |
| WPF-UI 4.2.0 | Componentes de interfaz |
| CommunityToolkit.Mvvm | Herramientas para aplicaciones MVVM |

## Arquitectura

El proyecto utiliza una separación entre la interfaz gráfica y la lógica de presentación mediante el patrón **MVVM**.

```text
SadViewer/
│
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
│
├── Converters/
│   └── ...
│
├── Models/
│   └── ...
│
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── BlockViewModel.cs
│   ├── PieceViewModel.cs
│   └── ...
│
├── Themes/
│   └── AppStyles.xaml
│
├── Assets/
│   └── ...
│
├── SADConfig.json
├── SadViewer.csproj
├── libSad64.dll
│
└── SadViewer.Tests/
    └── SadViewer.Tests.csproj
```

*La estructura representa los componentes principales del proyecto; los directorios pueden contener archivos adicionales.*

### Flujo de funcionamiento

1. La aplicación carga `SADConfig.json`.
2. Detecta los puertos COM disponibles.
3. Carga los archivos HRL y GHT seleccionados.
4. Inicializa la base de datos SAD.
5. Abre el puerto serie.
6. Inicializa el SDK mediante `Sad.Init()`.
7. Establece la conexión con el sistema electrónico.
8. Registra el callback de cambios de piezas.
9. Actualiza los datos de la interfaz cuando se reciben cambios.
10. Libera los recursos al desconectar o cerrar la aplicación.

Las operaciones de carga y conexión se ejecutan en segundo plano para evitar bloquear la interfaz.

## Requisitos

- Windows 10 u 11.
- Visual Studio 2026 o compatible con .NET 10.
- .NET 10 SDK.
- .NET 10 Desktop Runtime para ejecutar la aplicación publicada.
- Dispositivo electrónico SAD compatible.
- Puerto serie COM disponible.
- Biblioteca nativa `libSad64.dll`.
- Archivos de base de datos `.hrl` y `.ght`.

**Importante:** la biblioteca nativa debe ser compatible con la arquitectura del proceso. Para la distribución se recomienda compilar y ejecutar en x64.

## Configuración

La aplicación utiliza el archivo `SADConfig.json` para almacenar los parámetros de conexión y las rutas de la base de datos.

Ejemplo:

```json
{
  "COM": "COM3",
  "RATE": 115200,
  "DatabaseFolder": "SAD",
  "HRLFile": "Dron_Naval_1.hrl",
  "GHTFile": "Dron_Naval_1.ght"
}
```

### Parámetros

| Parámetro | Descripción |
|---|---|
| `COM` | Puerto serie utilizado para la conexión |
| `RATE` | Velocidad de comunicación en baudios |
| `DatabaseFolder` | Directorio de los archivos de base de datos |
| `HRLFile` | Archivo de definición HRL |
| `GHTFile` | Archivo complementario GHT |

Las rutas relativas se resuelven respecto al directorio de la aplicación.

La configuración se busca primero junto al ejecutable y, como alternativa, en:

```text
%APPDATA%\SadViewer\SADConfig.json
```

## Instalación y ejecución

### 1. Clonar el repositorio

```bash
git clone https://github.com/yandryrgp/SadViewer.git
```

### 2. Entrar en el directorio

```bash
cd SadViewer
```

### 3. Restaurar dependencias

```bash
dotnet restore SadViewer.csproj
```

### 4. Compilar

```bash
dotnet build SadViewer.csproj -c Release
```

### 5. Ejecutar

```bash
dotnet run --project SadViewer.csproj
```

También puedes abrir `SadViewer.csproj` directamente desde Visual Studio y ejecutar el proyecto.

## Pruebas

El repositorio incluye un proyecto de pruebas independiente basado en xUnit.

Para ejecutar las pruebas:

```bash
dotnet test SadViewer.Tests/SadViewer.Tests.csproj
```

## Publicación

Para generar una versión de distribución para Windows x64:

```bash
dotnet publish SadViewer.csproj -c Release -r win-x64 --self-contained true
```

Verifica que los siguientes recursos estén disponibles en el directorio de publicación:

- `libSad64.dll`.
- `SADConfig.json`.
- Archivos HRL y GHT correspondientes a la instalación.

La DLL nativa y las bases de datos deben distribuirse respetando sus respectivas licencias y condiciones de uso.

## Consideraciones técnicas

- La conexión con la electrónica depende del SDK SAD y de la disponibilidad del dispositivo.
- Los archivos HRL y GHT deben corresponder a la instalación y configuración del sistema.
- Los cambios de piezas se reciben mediante callbacks del SDK.
- Las actualizaciones de la interfaz se sincronizan con el hilo principal de WPF.
- Al cerrar la aplicación se intenta liberar la conexión y los recursos del SDK.

## Autor

**Yandry Roger García Pérez**

Desarrollo de software, integración de sistemas electrónicos y herramientas de supervisión.

## Licencia

No se especifica una licencia de código abierto en este README. Antes de distribuir el proyecto, debe definirse la licencia aplicable y comprobarse la autorización para redistribuir `libSad64.dll` y el SDK SAD.