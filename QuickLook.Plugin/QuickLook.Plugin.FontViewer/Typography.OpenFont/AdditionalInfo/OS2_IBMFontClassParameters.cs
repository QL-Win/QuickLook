//Apache2, 2017-present, WinterDev
//https://www.microsoft.com/typography/otspec/ibmfc.htm

namespace Typography.OpenFont
{

    //This section defines the IBM Font Class
    //and the IBM Font Subclass parameter values
    //to be used in the classification of font designs by the font designer or supplier.
    //
    //This information is stored in the sFamilyClass field of a font's OS/2 table. 


    [System.Flags]
    enum IBMFontClassParametersKind
    {
        No_Classification = 0 << 8,
        //
        //class id 1, OldStyle Serifs
        //
        Class1 = 1 << 8,
        OldStyle_Serifs = Class1,
        Class1_No_Classification = Class1 | 0,
        Class1_IBM_Rounded_Legibility = Class1 | 1,
        Class1_Garalde = Class1 | 2,
        Class1_Venetian = Class1 | 3,
        Class1_Modified_Venetian = Class1 | 4,
        Class1_Dutch_Modern = Class1 | 5,
        Class1_Dutch_Traditional = Class1 | 6,
        Class1_Comtemporary = Class1 | 7,
        Class1_Calligraphic = Class1 | 8,
        //subclass 9-14 ->  (reserved for future use)
        Class1_Miscellaneous = Class1 | 15,
        //
        //class id 2, Transitional Serifs        
        //
        Class2 = 2 << 8,
        Class2_No_Classification = Class2 | 0,
        Class2_Direct_Line = Class2 | 1,
        Class2_Script = Class2 | 2,
        //subclass 3-14 ->  (reserved for future use)
        Class2_Miscellaneous = Class2 | 15,
        //
        //class id 3, Modern Serifs        
        //
        //TODO... add more...


    }

}