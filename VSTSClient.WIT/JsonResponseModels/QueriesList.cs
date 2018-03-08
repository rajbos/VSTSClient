using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSTSClient.JsonResponseModels
{

    public class QueriesList
    {
        public int count { get; set; }
        public value[] value { get; set; }
    }

    public class value
    {
        public string id { get; set; }
        public string name { get; set; }
        public string path { get; set; }
        public DateTime createdDate { get; set; }
        public Lastmodifiedby lastModifiedBy { get; set; }
        public DateTime lastModifiedDate { get; set; }
        public bool isFolder { get; set; }
        public bool hasChildren { get; set; }
        public children[] children { get; set; }
        public bool isPublic { get; set; }
        public _Links _links { get; set; }
        public string url { get; set; }
        public Createdby createdBy { get; set; }
    }

    public class Lastmodifiedby
    {
        public string id { get; set; }
        public string name { get; set; }
        public string displayName { get; set; }
        public string uniqueName { get; set; }
    }

    public class _Links
    {
        public Self self { get; set; }
        public Html html { get; set; }
    }

    public class Self
    {
        public string href { get; set; }
    }

    public class Html
    {
        public string href { get; set; }
    }

    public class Createdby
    {
        public string id { get; set; }
        public string name { get; set; }
        public string displayName { get; set; }
        public string uniqueName { get; set; }
    }

    public class children
    {
        public string id { get; set; }
        public string name { get; set; }
        public string path { get; set; }
        public Createdby1 createdBy { get; set; }
        public DateTime createdDate { get; set; }
        public Lastmodifiedby1 lastModifiedBy { get; set; }
        public DateTime lastModifiedDate { get; set; }
        public string queryType { get; set; }
        public Column[] columns { get; set; }
        public string wiql { get; set; }
        public bool isPublic { get; set; }
        public Clauses clauses { get; set; }
        public Lastexecutedby lastExecutedBy { get; set; }
        public DateTime lastExecutedDate { get; set; }
        public _Links1 _links { get; set; }
        public string url { get; set; }
        public Sortcolumn[] sortColumns { get; set; }
        public Linkclauses linkClauses { get; set; }
        public string filterOptions { get; set; }
        public Sourceclauses sourceClauses { get; set; }
        public Targetclauses targetClauses { get; set; }
        public bool isInvalidSyntax { get; set; }
        public string queryRecursionOption { get; set; }
        public bool isFolder { get; set; }
        public bool hasChildren { get; set; }
    }

    public class Createdby1
    {
        public string id { get; set; }
        public string name { get; set; }
        public string displayName { get; set; }
        public string uniqueName { get; set; }
    }

    public class Lastmodifiedby1
    {
        public string id { get; set; }
        public string name { get; set; }
        public string displayName { get; set; }
        public string uniqueName { get; set; }
    }

    public class Clauses
    {
        public string logicalOperator { get; set; }
        public Claus[] clauses { get; set; }
        public Field field { get; set; }
        public Operator _operator { get; set; }
        public string value { get; set; }
    }

    public class Field
    {
        public string referenceName { get; set; }
        public string name { get; set; }
        public string url { get; set; }
    }

    public class Operator
    {
        public string referenceName { get; set; }
        public string name { get; set; }
    }

    public class Claus
    {
        public Field1 field { get; set; }
        public Operator1 _operator { get; set; }
        public string value { get; set; }
        public string logicalOperator { get; set; }
        public Claus1[] clauses { get; set; }
    }

    public class Field1
    {
        public string referenceName { get; set; }
        public string name { get; set; }
        public string url { get; set; }
    }

    public class Operator1
    {
        public string referenceName { get; set; }
        public string name { get; set; }
    }

    public class Claus1
    {
        public Field2 field { get; set; }
        public Operator2 _operator { get; set; }
        public string value { get; set; }
    }

    public class Field2
    {
        public string referenceName { get; set; }
        public string name { get; set; }
        public string url { get; set; }
    }

    public class Operator2
    {
        public string referenceName { get; set; }
        public string name { get; set; }
    }

    public class Lastexecutedby
    {
        public string id { get; set; }
        public string displayName { get; set; }
        public string uniqueName { get; set; }
        public string name { get; set; }
    }

    public class _Links1
    {
        public Self1 self { get; set; }
        public Html1 html { get; set; }
        public Parent parent { get; set; }
        public Wiql wiql { get; set; }
    }

    public class Self1
    {
        public string href { get; set; }
    }

    public class Html1
    {
        public string href { get; set; }
    }

    public class Parent
    {
        public string href { get; set; }
    }

    public class Wiql
    {
        public string href { get; set; }
    }

    public class Linkclauses
    {
        public string logicalOperator { get; set; }
        public Claus2[] clauses { get; set; }
    }

    public class Claus2
    {
        public string logicalOperator { get; set; }
        public Claus3[] clauses { get; set; }
        public Field3 field { get; set; }
        public Operator3 _operator { get; set; }
        public string value { get; set; }
    }

    public class Field3
    {
        public string referenceName { get; set; }
        public string name { get; set; }
        public string url { get; set; }
    }

    public class Operator3
    {
        public string referenceName { get; set; }
        public string name { get; set; }
    }

    public class Claus3
    {
        public Field4 field { get; set; }
        public Operator4 _operator { get; set; }
        public string value { get; set; }
    }

    public class Field4
    {
        public string referenceName { get; set; }
        public string name { get; set; }
        public string url { get; set; }
    }

    public class Operator4
    {
        public string referenceName { get; set; }
        public string name { get; set; }
    }

    public class Sourceclauses
    {
        public string logicalOperator { get; set; }
        public Claus4[] clauses { get; set; }
    }

    public class Claus4
    {
        public Field5 field { get; set; }
        public Operator5 _operator { get; set; }
        public string value { get; set; }
        public string logicalOperator { get; set; }
        public Claus5[] clauses { get; set; }
    }

    public class Field5
    {
        public string referenceName { get; set; }
        public string name { get; set; }
        public string url { get; set; }
    }

    public class Operator5
    {
        public string referenceName { get; set; }
        public string name { get; set; }
    }

    public class Claus5
    {
        public Field6 field { get; set; }
        public Operator6 _operator { get; set; }
        public string value { get; set; }
        public string logicalOperator { get; set; }
        public Claus6[] clauses { get; set; }
    }

    public class Field6
    {
        public string referenceName { get; set; }
        public string name { get; set; }
        public string url { get; set; }
    }

    public class Operator6
    {
        public string referenceName { get; set; }
        public string name { get; set; }
    }

    public class Claus6
    {
        public Field7 field { get; set; }
        public Operator7 _operator { get; set; }
        public string value { get; set; }
    }

    public class Field7
    {
        public string referenceName { get; set; }
        public string name { get; set; }
        public string url { get; set; }
    }

    public class Operator7
    {
        public string referenceName { get; set; }
        public string name { get; set; }
    }

    public class Targetclauses
    {
        public string logicalOperator { get; set; }
        public Claus7[] clauses { get; set; }
    }

    public class Claus7
    {
        public string logicalOperator { get; set; }
        public Claus8[] clauses { get; set; }
        public Field8 field { get; set; }
        public Operator8 _operator { get; set; }
        public string value { get; set; }
    }

    public class Field8
    {
        public string referenceName { get; set; }
        public string name { get; set; }
        public string url { get; set; }
    }

    public class Operator8
    {
        public string referenceName { get; set; }
        public string name { get; set; }
    }

    public class Claus8
    {
        public string logicalOperator { get; set; }
        public Claus9[] clauses { get; set; }
        public Field9 field { get; set; }
        public Operator9 _operator { get; set; }
        public string value { get; set; }
    }

    public class Field9
    {
        public string referenceName { get; set; }
        public string name { get; set; }
        public string url { get; set; }
    }

    public class Operator9
    {
        public string referenceName { get; set; }
        public string name { get; set; }
    }

    public class Claus9
    {
        public Field10 field { get; set; }
        public Operator10 _operator { get; set; }
        public string value { get; set; }
    }

    public class Field10
    {
        public string referenceName { get; set; }
        public string name { get; set; }
        public string url { get; set; }
    }

    public class Operator10
    {
        public string referenceName { get; set; }
        public string name { get; set; }
    }

    public class Column
    {
        public string referenceName { get; set; }
        public string name { get; set; }
        public string url { get; set; }
    }

    public class Sortcolumn
    {
        public Field11 field { get; set; }
        public bool descending { get; set; }
    }

    public class Field11
    {
        public string referenceName { get; set; }
        public string name { get; set; }
        public string url { get; set; }
    }

}
