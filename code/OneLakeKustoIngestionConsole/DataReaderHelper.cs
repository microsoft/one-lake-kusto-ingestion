using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLakeKustoIngestionConsole
{
    internal static class DataReaderHelper
    {
        public static T ToSingleValue<T>(this IDataReader reader)
        {
            return reader.ToSingleRow(r => (T)r[0]);
        }

        public static T ToSingleRow<T>(this IDataReader reader, Func<IDataReader, T> extractor)
        {
            return reader.ToRows(extractor).First();
        }

        public static IEnumerable<T> ToRows<T>(
            this IDataReader reader,
            Func<IDataReader, T> extractor)
        {
            while (reader.Read())
            {
                yield return extractor(reader);
            }
        }

    }
}