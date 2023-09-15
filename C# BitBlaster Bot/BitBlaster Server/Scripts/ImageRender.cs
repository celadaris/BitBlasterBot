using UnityEngine;
using UnityEngine.UI;

public class ImageRender : MonoBehaviour, IVideoPacket
{
    [SerializeField] RawImage image;
    Texture2D t;

    void Awake()
    {
        t = new Texture2D(120, 120);
    }

    public void VideoPacketRecieved(ENet.Event netEvent, VideoPacket videoPacket)
    {
        t.LoadImage(videoPacket.videoFeed);
        image.texture = t;
    }
}
