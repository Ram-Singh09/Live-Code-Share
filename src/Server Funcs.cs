using System.Collections.Concurrent;

// running backend
static class Server_Funcs
{
    // Vars -------------------------------------

    // Room id and room
    private static ConcurrentDictionary<int, Room_Info> Rooms = new();
    private static readonly Lock _Lock = new();
    private const byte Off_Time = 60;

    // Funcs -------------------------------------

    // ret Room joining info from endpoint mapping
    /// <param name="Create_info">From client</param>
    /// <param name="Admin_id">From server db</param>
    /// <param name="Admin_password">From server db</param>
    /// <param name="Join_info">back to client</param>
    public static Result_State Create_Room(Room_Creation_Info Create_info, int Admin_id, int Admin_password, out Room_Joining_Info Join_info)
    {
        lock (_Lock)
        {
            Join_info = default;

            if (Create_info.Admin_id != Admin_id || Create_info.Admin_password != Admin_password)
                return Result_State.Room_Creation_Failed;

            // find until un assigned key found
            int New_room_id = Random.Shared.Next();
            while (Rooms.ContainsKey(New_room_id)) { New_room_id = Random.Shared.Next(); }

            // set data for caller and add room
            Join_info = new(New_room_id, Random.Shared.Next());
            Rooms.TryAdd(New_room_id, new(Join_info));

            return Result_State.Success;
        }
    }

    // ret simple T/F
    public static Result_State Upload_File(Room_Joining_Info Info, byte[] Data)
    {

        if (!Rooms.TryGetValue(Info.Room_id, out var Room))
            return Result_State.Room_Operation_Failed;


        // is room valid anymore
        if (DateTime.UtcNow > Room.Last_Used.AddMinutes(Off_Time))
        {
            var Removed = Rooms.TryRemove(Info.Room_id, out var Temp);
            if (Removed) Temp.Dispose();

            return Result_State.Room_Closed;
        }

        if (!Room.Upload_File(Info, Data))
            return Result_State.Room_Operation_Failed;

        return Result_State.Success;
    }

    // ret file data and upload counter
    /// <param name="Data">File data filled in this buffer</param>>
    public static Result_State Get_File(Room_Joining_Info Info, out byte[] Data)
    {
        Data = null;

        if (!Rooms.TryGetValue(Info.Room_id, out var Room))
            return Result_State.Room_Operation_Failed;

        // is room valid anymore
        if (DateTime.UtcNow > Room.Last_Used.AddMinutes(Off_Time))
        {
            var Removed = Rooms.TryRemove(Info.Room_id, out var Temp);
            if (Removed) Temp.Dispose();

            return Result_State.Room_Closed;
        }

        byte[] Buffer = new byte[Room.Bytes_Length];

        if (!Room.Get_File(Info, Buffer))
            return Result_State.Room_Operation_Failed;

        Data = Buffer;
        return Result_State.Success;
    }

    public static void Update_Rooms()
    {
        lock (_Lock)
        {
            foreach (var i in Rooms)
            {
                if (DateTime.UtcNow > i.Value.Last_Used.AddMinutes(Off_Time))
                {
                    var Removed = Rooms.TryRemove(i.Value.Room_id, out var Temp);
                    if (Removed) Temp.Dispose();
                }
            }
        }
    }

}
