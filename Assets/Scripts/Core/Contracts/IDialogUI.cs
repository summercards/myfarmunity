namespace FarmGame.Core.Contracts
{
    public interface IDialogUI
    {
        bool IsOpen { get; }
        IDialogSubject CurrentSubject { get; }
        void Open(IDialogSubject subject);
        void Close();
    }
}
