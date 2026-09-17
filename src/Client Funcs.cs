using System.Text.Json;

// change file extension for specific needs
static class Client_Funcs
{
    // Vars --------------------------------

    public static Room_Joining_Info Room_join_info;

    private static HttpClient Client;
    public static JsonSerializerOptions Options;

    private const string Get_File_Name = "Remote Get.txt",
    Send_File_Name = "Remote Send.txt";
    private const int Max_Upload_Size = 1024 * 100;

    private static string File_extension = "cs",
    Remote_file_path = "";

    private static string Get_file_path = Remote_file_path + Get_File_Name,
    Send_file_path = Remote_file_path + Send_File_Name;

    // Funcs --------------------------------

    public static void Init()
    {
        if (!File.Exists(Get_file_path)) File.Create(Get_file_path).Dispose();
        if (!File.Exists(Send_file_path)) File.Create(Send_file_path).Dispose();
        
        // every x mins updates any dns update from server side
        Client = new(new SocketsHttpHandler()
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15)
        });
        Options = new() { IncludeFields = true, PropertyNameCaseInsensitive = true};
    }

    public static void Create_Room(Room_Creation_Info Info)
    {
        // create msg
        using var Req = new HttpRequestMessage(HttpMethod.Post, End_Point.Client_Create_Room);

        // add json of data req by this func on server 
        Req.Content = JsonContent.Create(Info, options:Options);

        // send create room req
        using var Res = Client.Send(Req);
        if (!Res.IsSuccessStatusCode)
        {
            Console.WriteLine("Failled to create room");
            return;
        }

        // when success it get room joining info from server
        var Res_arr = Res.Content.ReadAsByteArrayAsync().Result;

        // deserialzie what being received
        Room_join_info = JsonSerializer.Deserialize<Room_Joining_Info>(Res_arr, Options);

        Console.WriteLine("Room created successfully");
        Console.WriteLine("Room id - {0}, Pass - {1}", Room_join_info.Room_id, Room_join_info.Password);
    }

    public static void Send_File(Room_Joining_Info Info)
    {
        if (!File.Exists(Get_file_path))
        {
            File.Create(Get_file_path);
            Console.WriteLine("Cannot send file, file not exists.\nCreated new one");
        }

        // type of msg
        using var Req = new HttpRequestMessage(HttpMethod.Post, End_Point.Client_Upload_File);

        // no sending of empty array
        var File_bytes = File.ReadAllBytes(Send_file_path);
        if (File_bytes.Length == 0)
        {
            Console.WriteLine("Send file is empty, nothing to send");
            return;
        }

        // add json of data req by this func on server 
        Req.Content = JsonContent.Create
        (
            new Upload_File_Data(Info, File_bytes),
            options: Options
        );

        // send create room req
        using var Res = Client.Send(Req);
        if (!Res.IsSuccessStatusCode)
        {
            Console.WriteLine("Failled to send file - " + Res.ReasonPhrase);
            return;
        }

        // when success it gets upload counter
        var Res_arr = Res.Content.ReadAsByteArrayAsync().Result;

        Console.WriteLine("Sent file successfully");
    }

    public static void Get_File(Room_Joining_Info Info)
    {
        // type of msg
        using var Req = new HttpRequestMessage(HttpMethod.Post, End_Point.Client_Get_File);

        // add json of data req by this func on server 
        Req.Content = JsonContent.Create(Info, options:Options);

        // send create room req
        using var Res = Client.Send(Req);
        if (!Res.IsSuccessStatusCode)
        {
            Console.WriteLine("Failled to get file from server");
            return;
        }

        // take fs of content received form server
        using var Res_stream = Res.Content.ReadAsStream();
        using var Fs = new FileStream(Get_file_path, FileMode.Create, FileAccess.Write);
        
        // when success it gets fiel content
        Res_stream.CopyTo(Fs); // direct copy stream to stream

        Console.WriteLine("Received file successfully");
    }

    public static void Set_File_Extension(string X)
    {
        if (X is null || X.Length == 0)
        {
            Console.WriteLine("Extension provided is empty");
            return;
        }

        File_extension = X;
    }
    public static void Set_Remote_Folder(string X)
    {
        if (X is null || X.Length == 0)
        {
            Console.WriteLine("Path provided is empty");
            return;
        }

        Remote_file_path = X;
        Send_file_path = Remote_file_path + Send_File_Name;
        Get_file_path = Remote_file_path + Get_File_Name;

        if (!File.Exists(Get_file_path)) File.Create(Get_file_path).Dispose();
        if (!File.Exists(Send_file_path)) File.Create(Send_file_path).Dispose();
    }
}
