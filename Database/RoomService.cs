using chat_application.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace chat_application.Database
{
    public class RoomService
    {
        private readonly IMongoCollection<Room> _rooms;

        public RoomService(IDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _rooms = database.GetCollection<Room>(settings.RoomCollectionName);
        }

        public List<Room> Find(string name)
        {
            return _rooms.Find(room => room.Name == name).ToList();
        }

        public Room Get(string id) => _rooms.Find(room => room.Id == id).FirstOrDefault();

        public Room Create(Room room)
        {
            _rooms.InsertOne(room);
            return room;
        }

        public Room Update(Room room)
        {
            _rooms.ReplaceOne(r => r.Id == room.Id, room);
            return room;
        }

        public List<Room> GetAllRooms()
        {
            return _rooms.Find(room => true).ToList();
        }
    }
}
