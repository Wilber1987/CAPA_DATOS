using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CAPA_DATOS.BDCore.SQLServerImplementations
{
    public class SQLServerEntityQuerys
    {
        public static string DescribeEntityQuery = @"
            SELECT COLUMN_NAME, IS_NULLABLE, DATA_TYPE, TABLE_SCHEMA
            FROM [INFORMATION_SCHEMA].[COLUMNS] 
            WHERE [TABLE_NAME] = 'entityName' order by [ORDINAL_POSITION]
        ";
    }
}