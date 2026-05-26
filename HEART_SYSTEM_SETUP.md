# Sistema de Vida con Corazones - Guía de Configuración

## Cambios Realizados

### 1. **Script Player.cs** ✅
- Cambiado el sistema de vida de barra de vida (`vidaMaxima = 100f`) a **10 corazones**
- El daño del esqueleto es 40, que equivale a **2 corazones** por ataque
- Agregado un método público `GetCorazonesActuales()` para acceder a los corazones actuales

### 2. **Script ArmSkeleton.cs** ✅
- Aumentado el daño de 20 a **40** (para quitar 2 corazones)
- El cálculo es: 40 daño ÷ 20 (daño por corazón) = 2 corazones perdidos

### 3. **Script HeartDisplay.cs** ✅ (Nuevo)
- Maneja la visualización de los 10 corazones en la interfaz
- Lee automáticamente los corazones del jugador en cada frame
- Muestra corazones llenos o vacíos según la vida actual

Ahora necesitas configurar la UI en el editor de Unity. Sigue estos pasos:

#### Paso 1: Importar la imagen del corazón
- Ya existe en: `Assets/final Sprites Alvaro/ESCENARIO/ICONOS/heart.png`
- Asegúrate de que está configurada como Sprite en sus propiedades

#### Paso 2: Crear la UI de Corazones
1. En la jerarquía de la escena, crea un nuevo `Panel` vacío como contenedor de la UI
   - Nombra: `HeartContainer`
   - Posiciona en la esquina superior izquierda

2. Dentro de `HeartContainer`, crea 10 `Image` (UI)
   - Nombre: `Heart_0`, `Heart_1`, ... `Heart_9`
   - Tamaño: ~40x40 píxeles (o el que prefieras)
   - Espaciado horizontal: ~10 píxeles entre ellas

3. Para cada imagen:
   - En la propiedad `Image`, asigna el sprite `heart.png`
   - Source Image = `heart.png`

#### Paso 3: Configurar el Script Player
1. Selecciona el GameObject del jugador en la jerarquía
2. En el inspector, dentro del componente `Player`:
   - **Corazones Maximos**: 10 (ya está por defecto)
   - **Corazones**: Arrastra los 10 Image (Heart_0 a Heart_9) al array
   - **Corazon Lleno**: Asigna el sprite `heart.png` (o una versión llena)
   - **Corazon Vacio**: Asigna un sprite vacío (puedes crear uno con fondo transparente)

#### Paso 4: Configurar el Script HeartDisplay (Alternativa)
Si prefieres usar el script `HeartDisplay.cs`:

1. Crea un nuevo GameObject vacío en la escena: `HeartDisplayController`
2. Agrega el componente `HeartDisplay`
3. En el inspector:
   - **Player**: Arrastra el jugador (se auto-detecta si no lo haces)
   - **Corazones**: Arrastra los 10 Image (Heart_0 a Heart_9)
   - **Corazon Lleno**: Asigna `heart.png` lleno
   - **Corazon Vacio**: Asigna una imagen vacía

### 4. **Verificación del Sistema**

En el juego:
- Presiona juego
- El jugador comienza con **10 corazones**
- Cuando el esqueleto golpea al jugador: **20 de daño = 2 corazones perdidos**
- Cuando se llega a 0 corazones: **Game Over**

### 5. **Sprites Necesarios**

Para una mejor experiencia visual, puedes crear o usar:
- **Corazón lleno**: El actual `heart.png`
- **Corazón vacío**: Una versión en blanco/gris o un sprite del mismo tamaño pero transparente

## Fórmula del Daño (CONFIGURADA) ✅

```
Daño esqueleto = 40 (configurado en ArmSkeleton.cs)
Daño por corazón = 20
Corazones perdidos = ceil(40 / 20) = 2 corazones ✅
```
