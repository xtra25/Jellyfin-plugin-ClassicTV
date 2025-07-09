# ClassicTV Plugin para Jellyfin

Un plugin para Jellyfin que permite mezclar episodios de múltiples series seleccionadas en una playlist ordenada, manteniendo el orden interno de cada serie usando un algoritmo round-robin.

## Características

- ✅ **Selección múltiple de series**: Elige varias series desde la interfaz de configuración
- ✅ **Algoritmo round-robin**: Mezcla episodios alternando entre series
- ✅ **Respeto al orden interno**: Mantiene el orden cronológico de cada serie
- ✅ **Filtrado por usuario**: Solo incluye episodios no vistos por cada usuario
- ✅ **Playlists personalizadas**: Crea una playlist única para cada usuario
- ✅ **Tarea programada**: Genera playlists automáticamente o manualmente

## Instalación

### Requisitos
- Jellyfin Server 10.9.0 o superior
- .NET 8.0 Runtime

### Pasos de instalación

1. **Descargar el plugin**
   - Descarga el archivo `Jellyfin.Plugin.ClassicTV.dll` de la sección de releases
   - O compila desde el código fuente (ver sección de desarrollo)

2. **Instalar en Jellyfin**
   - Detén el servidor Jellyfin
   - Copia el archivo `Jellyfin.Plugin.ClassicTV.dll` a la carpeta de plugins:
     - **Windows**: `%PROGRAMDATA%\Jellyfin\Server\plugins`
     - **Linux**: `/var/lib/jellyfin/plugins`
     - **Docker**: Monta la carpeta de plugins en el contenedor
   - Reinicia el servidor Jellyfin

3. **Verificar la instalación**
   - Ve a **Dashboard** → **Plugins**
   - Busca "ClassicTV" en la lista de plugins instalados
   - El plugin debería aparecer como "ClassicTV" en la categoría "General"

## Configuración

### Configurar series seleccionadas

1. **Acceder a la configuración**
   - Ve a **Dashboard** → **Plugins**
   - Busca "ClassicTV" y haz clic en **Ajustes**

2. **Seleccionar series**
   - En la página de configuración, verás un selector múltiple con todas tus series
   - Mantén pulsado **Ctrl** (Windows) o **Cmd** (Mac) para seleccionar múltiples series
   - También puedes usar **Shift** para seleccionar rangos
   - Haz clic en **Guardar** para confirmar la selección

### Generar playlist

1. **Ejecutar la tarea manualmente**
   - Ve a **Dashboard** → **Tareas programadas**
   - Busca "Generar playlist ClassicTV"
   - Haz clic en **Ejecutar ahora**

2. **Verificar la playlist**
   - Ve a **Mi biblioteca** → **Playlists**
   - Busca la playlist "ClassicTV Playlist - [TuUsuario]"
   - La playlist contendrá episodios mezclados de las series seleccionadas

## Uso

### Cómo funciona el algoritmo

El plugin mezcla episodios usando un algoritmo **round-robin**:

1. **Ordena episodios**: Cada serie mantiene su orden cronológico interno
2. **Filtra no vistos**: Solo incluye episodios que el usuario no ha visto
3. **Mezcla alternando**: Toma un episodio de cada serie en rotación
4. **Ejemplo**:
   ```
   Serie A: Ep1, Ep2, Ep3, Ep4
   Serie B: Ep1, Ep2
   Serie C: Ep1, Ep2, Ep3

   Resultado: A1, B1, C1, A2, B2, C2, A3, C3, A4
   ```

### Características de la playlist

- **Personalizada por usuario**: Cada usuario tiene su propia playlist
- **Solo episodios no vistos**: No incluye episodios ya vistos
- **Límite de 1000 episodios**: Para evitar problemas de rendimiento
- **Orden round-robin**: Mantiene el equilibrio entre series

## Desarrollo

### Compilar desde el código fuente

1. **Clonar el repositorio**
   ```bash
   git clone https://github.com/tu-usuario/jellyfin-plugin-ClassicTV.git
   cd jellyfin-plugin-ClassicTV
   ```

2. **Requisitos de desarrollo**
   - .NET 8.0 SDK
   - Visual Studio 2022 o VS Code

3. **Compilar**
   ```bash
   cd Jellyfin.Plugin.ClassicTV
   dotnet build
   ```

4. **Instalar**
   - El archivo DLL se genera en `bin/Debug/net8.0/`
   - Copia `Jellyfin.Plugin.ClassicTV.dll` a la carpeta de plugins de Jellyfin

### Estructura del proyecto

```
Jellyfin.Plugin.ClassicTV/
├── Plugin.cs                    # Clase principal del plugin
├── Configuration/
│   ├── PluginConfiguration.cs   # Configuración del plugin
│   └── configPage.html         # Página de configuración web
├── EpisodeFetcher.cs           # Obtiene episodios de las series
├── EpisodeMixer.cs             # Algoritmo de mezcla round-robin
├── GeneratePlaylistTask.cs     # Tarea programada para generar playlists
└── Jellyfin.Plugin.ClassicTV.csproj
```

## Solución de problemas

### La página de configuración no aparece
- Verifica que el plugin esté instalado correctamente
- Revisa los logs de Jellyfin para errores
- Asegúrate de que el archivo DLL esté en la carpeta correcta

### La playlist solo tiene pocos episodios
- Verifica que las series seleccionadas tengan episodios no vistos
- Comprueba que los episodios no estén marcados como vistos
- Revisa los logs para ver cuántos episodios se encontraron

### Error al generar la playlist
- Verifica que las series seleccionadas existan en tu biblioteca
- Comprueba que tengas permisos para crear playlists
- Revisa los logs de Jellyfin para errores específicos

### La playlist no se actualiza
- Ejecuta manualmente la tarea "Generar playlist ClassicTV"
- Verifica que las series seleccionadas no hayan cambiado
- Comprueba que haya episodios no vistos disponibles

## Logs

Para diagnosticar problemas, revisa los logs de Jellyfin. El plugin registra información detallada sobre:

- Series seleccionadas
- Episodios encontrados por serie
- Episodios no vistos por usuario
- Proceso de mezcla round-robin
- Creación de playlists

## Contribuir

1. Fork el repositorio
2. Crea una rama para tu feature (`git checkout -b feature/nueva-funcionalidad`)
3. Commit tus cambios (`git commit -am 'Agregar nueva funcionalidad'`)
4. Push a la rama (`git push origin feature/nueva-funcionalidad`)
5. Crea un Pull Request

## Licencia

Este proyecto está licenciado bajo la Licencia MIT - ver el archivo [LICENSE](LICENSE) para detalles.

## Soporte

Si tienes problemas o preguntas:

1. Revisa la sección de [Solución de problemas](#solución-de-problemas)
2. Busca en los [Issues](https://github.com/tu-usuario/jellyfin-plugin-ClassicTV/issues)
3. Crea un nuevo issue con:
   - Versión de Jellyfin
   - Sistema operativo
   - Logs relevantes
   - Descripción detallada del problema

---

**¡Disfruta de tu experiencia de TV clásica mezclada!** 📺✨
