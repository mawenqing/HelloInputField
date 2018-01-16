namespace HelloInputField
{
    public interface ICaretNavigator
    {
        void MoveCaretTo(int index, bool withSelection);
    }
}
