using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// En WebGL, el VideoPlayer no puede cargar clips asignados directamente.
/// Este componente redirige el origen del video a una URL usando StreamingAssets,
/// que es el único método confiable para reproducir video en WebGL.
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public class WebGLVideoLoader : MonoBehaviour
{
    [Tooltip("Nombre del archivo de video dentro de la carpeta StreamingAssets (por ejemplo: VideoFondoMenu.mp4)")]
    public string videoFileName = "VideoFondoMenu.mp4";

    private void Start()
    {
        VideoPlayer vp = GetComponent<VideoPlayer>();

        // En WebGL siempre usamos URL. En el editor usamos el VideoClip asignado normalmente.
#if UNITY_WEBGL && !UNITY_EDITOR
        vp.source = VideoSource.Url;
        vp.url = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);
        vp.Prepare();
        vp.prepareCompleted += OnPrepared;
#else
        // En el editor/PC: si ya tiene un clip asignado lo dejamos como está.
        if (vp.source == VideoSource.VideoClip && vp.clip != null)
        {
            if (vp.playOnAwake)
                vp.Play();
        }
#endif
    }

    private void OnPrepared(VideoPlayer vp)
    {
        vp.Play();
    }
}
