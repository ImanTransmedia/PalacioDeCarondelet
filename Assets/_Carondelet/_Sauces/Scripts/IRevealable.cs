using UnityEngine;

public interface IRevealable
{
    void Reveal(bool visible);
}

[RequireComponent(typeof(Renderer))]
public class RevealableObject : MonoBehaviour, IRevealable
{
    private Renderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _renderer.enabled = false; // inicia oculto
    }

    public void Reveal(bool visible)
    {
        _renderer.enabled = visible;
    }
}
