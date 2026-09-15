using System.Runtime.InteropServices;

public struct Room_Creation_Info 
{
    public int Admin_id { get; set; }
    public int Admin_password { get; set; }
    
    public Room_Creation_Info(){}
    public Room_Creation_Info(int Admin_id, int Admin_password)
    {
        this.Admin_id = Admin_id;
        this.Admin_password = Admin_password;
    }
}
public struct Room_Joining_Info
{
    public int Room_id { get; set; }
    public int Password { get; set; }

    public Room_Joining_Info() { }
    
    public Room_Joining_Info (int Room_id, int Password)
    {
        this.Room_id = Room_id;
        this.Password = Password;
    }
}
public record Upload_File_Data(Room_Joining_Info Room_info, byte[] File_data);

unsafe sealed class Room_Info : IDisposable
{
    // Vars --------------------------------

    private byte* Data;
    private int Length;
    private const int Allocated_len = 1024 * 100;

    public readonly int Room_id, Password;

    private DateTime Last_used; // deletes room if inactive for long time
    private readonly Lock _Lock;

    // Funcs -------------------------------

    public Room_Info(Room_Joining_Info Info)
    {
        Length = 0; // how much being used
        Data = (byte*)NativeMemory.AllocZeroed((nuint)Allocated_len);

        Room_id = Info.Room_id;
        Password = Info.Password;

        Last_used = DateTime.UtcNow;
        _Lock = new();
    }

    public bool Upload_File(in Room_Joining_Info Key, byte[] Bytes)
    {
        lock (_Lock)
        {
            // validation
            if (Data is null || Key.Room_id != Room_id || Key.Password != Password ||
            Bytes is null || Bytes.Length == 0 || Bytes.Length > Allocated_len)
                return false;

            // copy
            Length = Bytes.Length;
            Bytes.AsSpan().CopyTo(new Span<byte>(Data, Length));

            // for maintaince
            Last_used = DateTime.UtcNow;

            return true;
        }
    }

    // pre alloc buffer to get data
    public bool Get_File(in Room_Joining_Info Key, byte[] Bytes)
    {
        lock (_Lock)
        {
            // few checks
            if (Data is null || Key.Room_id != Room_id || Key.Password != Password ||
            Bytes is null || Bytes.Length < Length) return false;

            new Span<byte>(Data, Length).CopyTo(Bytes);
            Last_used = DateTime.UtcNow;

            return true;
        }
    }

    public int Bytes_Length
    {
        get { lock (_Lock) return Length; }
    }
    public DateTime Last_Used { get => Last_used; }

    public void Dispose()
    {
        lock (_Lock)
        {
            // do not call after 1 time its waste of power
            if (Data is null) return;

            NativeMemory.Free(Data);
            Data = null;
            GC.SuppressFinalize(this); // will not run destructor
        }
    }
    ~Room_Info() => Dispose();
}
