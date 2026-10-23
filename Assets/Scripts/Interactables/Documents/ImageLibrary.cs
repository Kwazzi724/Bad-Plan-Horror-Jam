using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ImageLibrary", menuName = "Game/Image Library")]
public class ImageLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string id;
        public Sprite sprite;
    }

    [SerializeField] private List<Entry> images = new List<Entry>();

    public bool TryGet(string id, out Sprite sprite)
    {
        Entry entry = images.Find(image => image.id == id);
        sprite = entry != null ? entry.sprite : null;
        return sprite != null;
    }
}
