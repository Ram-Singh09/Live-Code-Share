
static class End_Point
{
    public const string
    Server_Create_Room = "/CodeShare/CreateRoom", // POST
    Server_Upload_File = "/CodeShare/Room/UploadFile", // POST
    Server_Get_File = "/CodeShare/Room/GetFile"; // POST
}

enum Result_State : byte
{
    Success, Room_Creation_Failed,
    Failed, Room_Operation_Failed,
    Room_Closed, File_Too_Big
}

static class Headers
{
    public const string Admin_Info = "AdminInfo",
    Room_Credentials = "RoomCredentials",
    File_Data = "FileData",
    File_And_Credentials = "FileAndCredentials";
}
