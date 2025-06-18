using System.Globalization;

namespace App.Models;

public class PersianDateProcess
{
    public bool TryConvertToDateTime(string? persianDate, out DateTime? dateTime)
    {
        dateTime = null;
        bool result = false;
        if (string.IsNullOrWhiteSpace(persianDate))
        {
            return false;
        }

        if (!persianDate.Contains('/'))
        {
            return false;
        }

        string[] persianDateArray = persianDate.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (persianDateArray.Length != 3)
        {
            return false;
        }

        string persianYearString = persianDateArray[0];
        string persianMonthString = persianDateArray[1];
        string persianDayString = persianDateArray[2];

        if (int.TryParse(persianYearString, out int persianYear) &&
            int.TryParse(persianMonthString, out int persianMonth) &&
            int.TryParse(persianDayString, out int persianDay))
        {
            try
            {
                dateTime =
                new PersianCalendar().ToDateTime(persianYear, persianMonth, persianDay, 0, 0, 0, 0);
                result = true;
            }
            catch //(ArgumentOutOfRangeException e)
            {
                //log
            }
        }
        return result;
    }
}