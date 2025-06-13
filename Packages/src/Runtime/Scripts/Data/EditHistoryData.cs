public class EditHistoryData
{
    public string Text;
    public int StringPosition;

    public EditHistoryData(string text, int stringPosition)
    {
        Text = text;
        StringPosition = stringPosition;
    }

    public override string ToString()
    {
        return $"Text: {Text}, StringPosition: {StringPosition}";
    }
}
