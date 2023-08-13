using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class functions : MonoBehaviour
{
    public static int DR(int k)
    {
        if (k == 0) { return 0; }
        else if (k % 9 == 0) { return 9; }
        else { return k % 9; }
    }

    public static List<int> Factor(int k)
    {
        List<int> factors = new List<int>();
        int max = (int)Math.Sqrt(k);
        for (int factor = 1; factor <= max; ++factor) 
        {
            if (k % factor == 0)
            {
                factors.Add(factor);
                if (factor != k / factor)
                    factors.Add(k / factor);

            }
        }
        return factors;
    }

    public static int function(int input, int k)
    {
        StringBuilder sb = new StringBuilder();
        int output = 0;
        bool temp = false;
        while (output == 0)
        {
            if (temp) { input--; input = (input + 1000) % 1000; }
            switch (k)
            {
                case 0:
                    output = Convert.ToInt32(Math.Pow(Convert.ToDouble(input), 2)) + 1;
                    break;
                case 1:
                    output = Convert.ToInt32(Math.Pow(Convert.ToDouble(input), 3)) + 1;
                    break;
                case 2:
                    output = input + DR(input);
                    break;
                case 3:
                    output = 2 * input;
                    break;
                case 4:
                    output = 7 * input;
                    break;
                case 5:
                    output = input / 3;
                    break;
                case 6:
                    output = 999 - input;
                    break;
                case 7:
                    output = Math.Abs(input - Convert.ToInt32(Math.Pow(Convert.ToDouble(input), 3)));
                    break;
                case 8:
                    string binaryString = Convert.ToString(input, 2);
                    for (int i = 0; i < binaryString.Length; i++)
                    {
                        output += Convert.ToInt32((binaryString[binaryString.Length - 1 - i] - '0') * Math.Pow(3, i));
                    }
                    output++;
                    break;
                case 9:
                    output = Factor(input).Sum();
                    break;
                case 10:
                    for (int i = 0; i < input.ToString("000").Length; i++)
                    {
                        sb.Append((input.ToString("000")[i] - '0') + 1 == 10 ? 0 : (input.ToString("000")[i] - '0') + 1);
                    }
                    output = Convert.ToInt32(sb.ToString());
                    break;
                case 11:
                    output = Convert.ToInt32(Math.Ceiling(Math.Sqrt(Convert.ToDouble(input)))) + 1;
                    break;
                case 12:
                    for (int i = 0; i < input.ToString("000").Length; i++)
                    {
                        if (i == 0) { sb.Append(input.ToString("000")[input.ToString("000").Length - 1]); }
                        else { sb.Append(input.ToString("000")[i - 1]); }
                    }
                    output = Convert.ToInt32(sb.ToString());
                    break;
                case 13:
                    for (int i = 0; i < input.ToString("000").Length; i++)
                    {
                        sb.Append(input.ToString("000")[input.ToString("000").Length - 1 - i]);
                    }
                    output = Convert.ToInt32(sb.ToString());
                    break;
                case 14:
                    output = input;
                    break;
            }
            output = (output + 1000) % 1000;
            temp = true;
        }
        return output;
    }
}
