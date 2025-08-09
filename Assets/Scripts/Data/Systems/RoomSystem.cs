using UnityEngine;
using System.Collections.Generic;
using Helloop.Events;
using Helloop.Rooms;

namespace Helloop.Systems
{
    [CreateAssetMenu(fileName = "RoomSystem", menuName = "Helloop/Systems/RoomSystem")]
    public class RoomSystem : ScriptableObject
    {
        [Header("Events")]
        public GameEvent OnEntryRoomRegistered;
        public GameEvent OnRoomRegistered;
        public GameEvent OnGenerationComplete;

        [Header("Runtime State")]
        private List<RoomController> allRooms = new List<RoomController>();
        private EntryRoom entryRoom;

        public EntryRoom EntryRoom => entryRoom;
        public bool HasValidEntryRoom() => entryRoom != null;
        public List<RoomController> AllRooms => new List<RoomController>(allRooms);
        public int RoomCount => allRooms.Count;

        void OnEnable()
        {
            ClearRooms();
        }

        public void StartGeneration()
        {
            ClearRooms();
        }

        public void RegisterRoom(RoomController room)
        {
            if (room == null || allRooms.Contains(room)) return;

            allRooms.Add(room);

            if (room.GetType() == typeof(EntryRoom))
            {
                entryRoom = room as EntryRoom;
                OnEntryRoomRegistered?.Raise();
            }

            OnRoomRegistered?.Raise();
        }

        public void UnregisterRoom(RoomController room)
        {
            if (room == null) return;

            allRooms.Remove(room);

            if (room == entryRoom)
            {
                entryRoom = null;
            }
        }

        public void CompleteGeneration()
        {
            OnGenerationComplete?.Raise();
        }

        public void ClearRooms()
        {
            allRooms.Clear();
            entryRoom = null;
        }

    }
}