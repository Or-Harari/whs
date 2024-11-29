public class LoginRequest
{
    public string UserName { get; set; }
    public string Password { get; set; }

    // Parameterless constructor required for deserialization
    public LoginRequest() { }

    public LoginRequest(string userName, string password)
    {
        UserName = userName;
        Password = password;
    }
}
