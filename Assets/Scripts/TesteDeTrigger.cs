using UnityEngine;

public class TesteDeTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Se qualquer coisa entrar neste trigger, ele vai gritar no console.
        Debug.Log($"<color=lime><b>!!!! TRIGGER FUNCIONOU !!!! Objeto que entrou: {other.name}</b></color>");
    }
}