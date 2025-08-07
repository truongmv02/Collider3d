using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public struct Chunk<T>
{
    public Vector2Int position;
    public List<T> objectList;


    public Chunk(Vector2Int position)
    {
        this.position = position;
        objectList = new List<T>();
    }

    public void Add(T obj)
    {
        objectList.Add(obj);
    }

    public void Remove(T obj)
    {
        objectList.Remove(obj);
    }

    public void RemoveAtSwapBack(int index)
    {
        objectList.RemoveAtSwapBack(index);
    }

    public void RemoveSwapBack(T obj)
    {
        objectList.RemoveSwapBack(obj);
    }


    public void Clear()
    {
        objectList.Clear();
    }
}