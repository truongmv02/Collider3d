using Unity.Collections;

public static class HashMapExtension
{
    public static bool ContainsKeyValue(this NativeParallelMultiHashMap<int, int> map, int key, int value)
    {
        NativeParallelMultiHashMapIterator<int> it;
        int val;
        if (map.TryGetFirstValue(key, out val, out it))
        {
            do
            {
                if (val == value) return true;
            } while (map.TryGetNextValue(out val, ref it));
        }

        return false;
    }
}