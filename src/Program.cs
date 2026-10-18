
// builder setup
var Builder = WebApplication.CreateBuilder(args);
Builder.Services.ConfigureHttpJsonOptions((Options) => { Options.SerializerOptions.IncludeFields = true; });

// app setup
var Web_app = Builder.Build();
Web_app.UseDefaultFiles();
Web_app.UseStaticFiles();


        // create room
        Web_app.MapPost(End_Point.Server_Create_Room, () =>
        {
            var State = Server_Funcs.Create_Room(out var Room_join_info);

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

static string Error_Msg(Result_State X)
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
