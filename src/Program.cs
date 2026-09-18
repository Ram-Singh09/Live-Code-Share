using Microsoft.AspNetCore.RateLimiting;

static class Program
{

    public static async Task Main(string[] args)
    {
        // run func
        //Run_Server(args);

        Run_Client();
    }

    private static void Run_Server(string[] args)
    {
        var Builder = WebApplication.CreateBuilder(args);

        Builder.Services.ConfigureHttpJsonOptions((Options) => { Options.SerializerOptions.IncludeFields = true; });
        Builder.Services.AddRateLimiter(Opts =>
        {
            // req which needs auth, thier time window based limit
            Opts.AddFixedWindowLimiter("StrictAuth", Options =>
                {
                    Options.PermitLimit = 500; // req which need author. can be requested 5 times per given window time
                    Options.Window = TimeSpan.FromMinutes(1); // refresh after 1 min
                    Options.QueueLimit = 0; // no queing the requests
                });

            // how many req to handle ffrom 1 user at same time
            Opts.AddConcurrencyLimiter("SingleInFlight", Opts =>
            {
                Opts.PermitLimit = 500; // 1 req at time 
                Opts.QueueLimit = 0; // again no queing the reqs
            });

            // general api usage for all public(non auth end points)
            Opts.AddSlidingWindowLimiter("GeneralApi", Opts =>
            {
                Opts.PermitLimit = 100; // 50 reqs allowed 
                Opts.Window = TimeSpan.FromSeconds(10); // per 10 secs
                Opts.SegmentsPerWindow = 2; // divides window to segments of equal time
                Opts.QueueLimit = 0; // do not store multiple req from 1 suer
            });
        });

        // extract admin info from app settings
        var Admin_info = Builder.Configuration.GetSection("AdminInfo").Get<Room_Creation_Info>();

        var Web_app = Builder.Build();
        Web_app.UseRateLimiter();

        // create room
        Web_app.MapPost(End_Point.Server_Create_Room, (Room_Creation_Info Info) =>
        {
            var State = Server_Funcs.Create_Room(Info, Admin_info.Admin_id, Admin_info.Admin_password, out var Room_join_info);

            if (State != Result_State.Success)
                return Results.Problem(Error_Msg(State));

            return Results.Ok(Room_join_info);
        });

        // upload file
        Web_app.MapPost(End_Point.Server_Upload_File, (Upload_File_Data Data) =>
        {
            var State = Server_Funcs.Upload_File(Data.Room_info, Data.File_data);

            if (State != Result_State.Success) return Results.Problem(Error_Msg(State));

            return Results.Ok();
        });

        // give file to client
        Web_app.MapPost(End_Point.Server_Get_File, (Room_Joining_Info Info) =>
        {
            var State = Server_Funcs.Get_File(Info, out var Data);

            if (State != Result_State.Success) return Results.Problem(Error_Msg(State));

            // ok msg and sending bytes directly, no json conversion.
            return Results.Bytes(Data, contentType: "application/octet-stream"); // str tells its bytes data directly
        });

        Web_app.Run();
    }

    private static void Run_Client()
    {
        Console.WriteLine("Give folder path where you want to use remote files");
        var Remote_folder = Console.ReadLine();

        // remove "" or '
        Remote_folder = Remote_folder.Trim('"');

        if (!Directory.Exists(Remote_folder))
        {
            Console.WriteLine("Exiting program, folder provided does not exists");
            return;
        }

        if (Remote_folder[Remote_folder.Length - 1] != '\\')
            Remote_folder += '\\';

        Client_Funcs.Set_Remote_Folder(Remote_folder);
        Client_Funcs.Init();

        const string Send_Cmd = "send", Get_Cmd = "get", Create_Room_Cmd = "create room"
        , Room_Credentials_Cmd = "join room", Change_Folder = "change folder";

        bool Room_joined = false;
        Room_Joining_Info Room_join_info = default;

        Console.WriteLine("Type any thing except specific cmd to get cmds list");

        while (true)
        {
            var Cmd = Console.ReadLine()!.ToLower();

            switch (Cmd)
            {
                case Create_Room_Cmd:

                    Console.WriteLine("Type admin id -");
                    var Temp_id_str = Console.ReadLine();

                    Console.WriteLine("Type admin password -");
                    var Temp_password_str = Console.ReadLine();

                    if (string.IsNullOrEmpty(Temp_id_str))
                    {
                        Console.WriteLine("Admin id provided is empty");
                        break;
                    }
                    if (string.IsNullOrEmpty(Temp_password_str))
                    {
                        Console.WriteLine("Admin password provided is empty");
                        break;
                    }

                    if (!int.TryParse(Temp_id_str, out var Temp_id))
                    {
                        Console.WriteLine("Id is a number, provided input not acceptable");
                        break;
                    }
                    if (!int.TryParse(Temp_password_str, out var Temp_pass))
                    {
                        Console.WriteLine("Password is a number, provided input not acceptable");
                        break;
                    }

                    Client_Funcs.Create_Room(new(Temp_id, Temp_pass));
                    Room_join_info = Client_Funcs.Room_join_info;

                    break;

                case Send_Cmd:

                    Client_Funcs.Send_File(Room_join_info);
                    break;

                case Get_Cmd:

                    Client_Funcs.Get_File(Room_join_info);
                    break;

                case Room_Credentials_Cmd:

                    Console.WriteLine("Type room id -");
                    Temp_id_str = Console.ReadLine();

                    Console.WriteLine("Type password password -");
                    Temp_password_str = Console.ReadLine();

                    if (string.IsNullOrEmpty(Temp_id_str))
                    {
                        Console.WriteLine("Room id provided is empty");
                        break;
                    }
                    if (string.IsNullOrEmpty(Temp_password_str))
                    {
                        Console.WriteLine("room password provided is empty");
                        break;
                    }

                    if (!int.TryParse(Temp_id_str, out Temp_id))
                    {
                        Console.WriteLine("Id is a number, provided input not acceptable");
                        break;
                    }
                    if (!int.TryParse(Temp_password_str, out Temp_pass))
                    {
                        Console.WriteLine("Password is a number, provided input not acceptable");
                        break;
                    }

                    Room_join_info = new(Temp_id, Temp_pass);

                    break;

                case Change_Folder:

                    Console.WriteLine("Give folder path, without double quotes");
                    var New_folder = Console.ReadLine();

                    // validity
                    if (string.IsNullOrEmpty(New_folder))
                    {
                        Console.WriteLine("cannot take empty string as path");
                        break;
                    }

                    // make it usable
                    New_folder = New_folder.Trim(' ', '"');
                    
                    // just check and skip do not create new folders
                    if (!Directory.Exists(New_folder))
                    {
                        Console.WriteLine("Folder change canceled, folder provided does not exists");
                        break;
                    }

                    Client_Funcs.Set_Remote_Folder(New_folder);
                    Remote_folder = New_folder;

                    break;

                default:
                    Console.WriteLine($"Invalid cmd, available cmds -\nsend - to send file\nget - to get file\ncreate room - for creating room\njoin room - for joining room\nchange folder - for changing remote folder");
                    break;
            }
        }
    }

    private static string Error_Msg(Result_State X)
    {
        return X switch
        {
            Result_State.Room_Creation_Failed => "Room cannot be created",
            Result_State.Failed => "Failed",
            Result_State.Room_Operation_Failed => "Room related action failed",
            Result_State.File_Too_Big => "File size too big",
            _ => "Error occured"
        };
    }

}
