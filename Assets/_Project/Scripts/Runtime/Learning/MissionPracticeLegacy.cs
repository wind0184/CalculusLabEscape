namespace CalculusLabEscape
{
    // Version zero is retained for practice runs already saved before the revision.
    public static partial class MissionPractice
    {
        static PracticeQuestion[] LegacyQuestions(string focus)
        {
            switch(focus)
            {
                case "rewriting":return new[]{
                    Q("Rewrite the cube root of x as a power.",1,"The cube root is x^(1/3). The root index becomes the denominator of the exponent.","x^3","x^(1/3)","x^(-3)"),
                    Q("Rewrite 1/x^4 as a power.",2,"A reciprocal changes the exponent's sign: 1/x^4 = x^(-4), for x != 0.","x^(1/4)","x^4","x^(-4)"),
                    Q("Rewrite 2/sqrt(x) as a power, for x > 0.",0,"Keep the coefficient 2. The reciprocal square root contributes exponent -1/2.","2x^(-1/2)","2x^(1/2)","x^(-2)")};
                case "constants":return new[]{
                    Q("Differentiate f(x) = 9.",2,"A constant has no change as x varies, so its derivative is 0.","9","9x","0"),
                    Q("Differentiate f(x) = -7x.",0,"Multiply -7 by 1, then use x^0 = 1. The derivative is -7.","-7","-7x","0"),
                    Q("Differentiate f(x) = 4x + 9.",1,"The linear term contributes 4 and the constant contributes 0.","4x","4","13")};
                case "integration":return new[]{
                    Q("Choose an antiderivative of 6x^2.",1,"Raise 2 to 3, then divide 6 by 3: 2x^3 + C. Its derivative is 6x^2.","6x^3 + C","2x^3 + C","12x + C"),
                    Q("Integrate x^(1/3), for x > 0.",0,"The new exponent is 4/3. Dividing by 4/3 gives coefficient 3/4.","(3/4)x^(4/3) + C","(4/3)x^(4/3) + C","(1/3)x^(-2/3) + C"),
                    Q("Integrate -8x^3.",2,"Raise 3 to 4 and divide -8 by 4, preserving the negative sign.","-8x^4 + C","-24x^2 + C","-2x^4 + C")};
                case "constant_c":return new[]{
                    Q("Which is the general indefinite integral of 8x?",2,"The derivative of 4x^2 + C is 8x for any constant C. The general answer needs +C.","4x^2","8x^2 + C","4x^2 + C"),
                    Q("Integrate 2x + 3. How should the constant appear?",0,"Integrate each term, then use one arbitrary constant: x^2 + 3x + C.","x^2 + 3x + C","x^2 + 3x","2x + 3 + C"),
                    Q("Evaluate the definite integral of 2x from 1 to 3.",1,"F(3) - F(1) = 9 - 1 = 8. Constants cancel when subtracting the bounds.","8 + C","8","9")};
                case "sequence":return new[]{
                    Q("To integrate the cube root of x, what comes first?",0,"Rewrite the root as x^(1/3), then apply the Power Rule.","Rewrite as x^(1/3)","Subtract 1 from the exponent","Drop the root symbol"),
                    Q("After rewriting x^(1/3), what is the raised exponent?",2,"Add one whole unit: 1/3 + 3/3 = 4/3.","-2/3","1/4","4/3"),
                    Q("After raising to 4/3, which finishes the integration?",1,"Divide by the new exponent, then add C: (3/4)x^(4/3) + C.","Multiply by 4/3 and stop","Divide by 4/3, then add C","Subtract 1, then add C")};
                case "bounds":return new[]{
                    Q("Evaluate the integral of 2x from 1 to 3.",1,"Use F(x) = x^2. F(3) - F(1) = 9 - 1 = 8.","9","8","10"),
                    Q("Evaluate the integral of 3x^2 from 0 to 2.",2,"Use F(x) = x^3. F(2) - F(0) = 8 - 0 = 8.","12","4","8"),
                    Q("Evaluate the integral of 4 from 2 to 5.",0,"Use F(x) = 4x. Subtract the lower-bound value: 20 - 8 = 12.","12","20","28")};
                default:return new[]{
                    Q("Differentiate f(x) = 3x^(-2), for x != 0.",2,"Multiply 3 by -2 to get -6. Subtract one from -2 to get -3.","-6x^(-1)","3x^(-3)","-6x^(-3)"),
                    Q("Differentiate f(x) = x^(1/3), for x > 0.",0,"Multiply by 1/3, then subtract one: 1/3 - 1 = -2/3.","(1/3)x^(-2/3)","(1/3)x^(4/3)","3x^(-2/3)"),
                    Q("Differentiate f(x) = 2x^4 - 5x.",1,"Differentiate term by term: 2(4)x^3 - 5 = 8x^3 - 5.","8x^3 - 5x","8x^3 - 5","2x^3 - 5")};
            }
        }
    }
}
