using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TeamG.Data
{
    /// <summary>マスターデータの1行。Id で引けるようにする。</summary>
    public interface IMasterRecord
    {
        int Id { get; }
    }

    /// <summary>エディタの変換ツールが型を知らなくても中身を入れられるようにするための非ジェネリック基底。</summary>
    public abstract class MasterTableBase : ScriptableObject
    {
        public abstract Type RecordType { get; }

#if UNITY_EDITOR
        public abstract void EditorSetRecords(IList records);
#endif
    }

    /// <summary>
    /// マスターデータ1テーブル分のアセット。中身は MasterData/*.csv から自動で作られる。
    /// </summary>
    public abstract class MasterTable<T> : MasterTableBase where T : class, IMasterRecord
    {
        [SerializeField] List<T> records = new List<T>();

        Dictionary<int, T> byId;

        public override Type RecordType => typeof(T);

        public IReadOnlyList<T> All => records;

        public int Count => records.Count;

        public T Get(int id)
        {
            if (TryGet(id, out var record))
            {
                return record;
            }
            throw new KeyNotFoundException($"{name}: Id {id} のデータはありません");
        }

        public bool TryGet(int id, out T record)
        {
            byId ??= BuildIndex();
            return byId.TryGetValue(id, out record);
        }

        Dictionary<int, T> BuildIndex()
        {
            var index = new Dictionary<int, T>(records.Count);
            foreach (var record in records)
            {
                index[record.Id] = record;
            }
            return index;
        }

#if UNITY_EDITOR
        public override void EditorSetRecords(IList list)
        {
            records = new List<T>(list.Count);
            foreach (var item in list)
            {
                records.Add((T)item);
            }
            byId = null;
        }
#endif
    }
}
