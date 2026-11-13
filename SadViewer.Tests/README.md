# Pruebas automatizadas de SadViewer

Proyecto de pruebas xUnit para `SadViewer`, dirigido a `net10.0-windows` porque referencia la aplicación WPF.

## Ejecutar desde PowerShell

```powershell
dotnet test SadViewer.Tests\SadViewer.Tests.csproj
```

## Cobertura inicial

- Formato de `LogLine`.
- Valores predeterminados de `SADConfig`.
- Ejecución de `RelayCommand`.
- Evaluación de `CanExecute` en `RelayCommand`.
- Filtrado de entradas del log por nivel y texto.

Las pruebas de conexión serie, `libSad64.dll` y hardware físico quedan fuera de esta primera fase y deberán cubrirse posteriormente con adaptadores simulables o pruebas de integración.
