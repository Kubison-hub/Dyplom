using UnityEngine;

public class ObjectDescription : MonoBehaviour
{
    [Header("Opis do Tooltipa")]
    [TextArea] // Powiêksza okienko w Inspektorze dla d³u¿szych tekstów
    public string description = "Wpisz nazwê obiektu";
}