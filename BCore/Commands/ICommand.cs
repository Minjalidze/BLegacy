using BCore.Users;

namespace BCore.Commands;

public interface ICommand
{
    public string CmdName { get; }

    public string[] RuDescription { get; set; }
    public string[] EngDescription { get; set; }

    public int[] Ranks { get; set; }

    public void Execute(NetUser user, string cmd, string[] args, User userData = null);
}