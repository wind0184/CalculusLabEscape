namespace CalculusLabEscape
{
    public static class MissionHints
    {
        public const string DerivativeRule="Power Rule: d(a*x^n)/dx = (a*n)x^(n-1). Rewrite roots and reciprocals as powers first.";
        public const string IntegralRule="For n != -1, integral of a*x^n dx = a*x^(n+1)/(n+1) + C. Raise the exponent, then divide by the new exponent.";
        public const string BoundsRule="Find an antiderivative F first. A definite integral is F(upper) - F(lower); the arbitrary constants cancel.";
        const string OrdinaryPowerRule="Power Rule: d(a*x^n)/dx = (a*n)x^(n-1). Multiply the original coefficient by the exponent, then subtract one from the exponent.";
        const string NegativePowerRule="The Power Rule also applies to negative exponents. Multiply the coefficient by the original negative exponent, then subtract one from that exponent. Track both signs separately.";
        public static string DerivativeRuleFor(string id)
        {
            if(id=="DX_BASIC_1")return OrdinaryPowerRule;
            if(id=="DX_BASIC_2")return NegativePowerRule;
            if(id=="DX_BASIC_3")return "Root rule: the kth root of x^m is x^(m/k). The inside power is the numerator; the root index is the denominator. Then multiply by this exponent and subtract one for the derivative.";
            if(id=="DX_BASIC_4")return "Rewrite a/(b*x^n) as (a/b)x^(-n). Keep the numerical fraction, multiply it by the negative exponent, then subtract one from that exponent.";
            if(id=="DX_APPLY_2")return "Split the fraction into separate terms. When dividing powers of x, subtract the denominator exponent from each numerator exponent: x^p / x^q = x^(p-q). Then apply the Power Rule to each term.";
            if(id=="DX_APPLY_3")return "Divide every numerator term by the entire denominator, including its numerical coefficient. Subtract the denominator exponent, rewrite using fractional exponents, then differentiate each term while preserving signs.";
            return DerivativeRule;
        }
        public static string Bridge(int stage,int tier)
        {
            if(tier==1)
            {
                if(stage==0||stage==4)return OrdinaryPowerRule;
                if(stage==2)return "The Power Rule applies to fractional exponents. Multiply the original coefficient by the fraction, then subtract one whole unit using a common denominator.";
                if(stage==5)return NegativePowerRule;
                if(stage==7)return "Differentiate each term separately using the Power Rule. The derivative of a constant is zero. Combine the results while keeping the original signs.";
                if(stage==10)return "Differentiate each term separately. Apply the Power Rule to positive and negative exponents, use the constant coefficient for a linear term, and combine the derivatives with the correct signs.";
                return stage==1?"The nth root of x^m is x^(m/n).":stage==8?"A reciprocal power becomes a negative exponent: 1/x^n = x^(-n), for x != 0.":stage==6?"A constant does not vary with x. Its derivative measures that change.":stage==9?"For a linear term a*x, use exponent 1 in the Power Rule.":DerivativeRule;
            }
            if(tier==2)return stage==1?"Similar example: the cube root of x^2 is x^(2/3). The root index supplies the denominator.":stage==8?"Similar example: 1/x^7 = x^(-7), for x != 0. Moving the factor above the fraction changes the exponent's sign.":stage==6?"Similar example: f(x) = 12 has the same output at every x, so its rate of change is zero.":stage==9?"Similar example: d(-9x)/dx = -9(1)x^0 = -9.":stage==3||stage==5?"Similar example: d(4x^(-5))/dx = 4(-5)x^(-6) = -20x^(-6). Keep the signs in both calculations.":stage==2?"Similar example: d(x^(1/4))/dx = (1/4)x^(-3/4). Subtract one whole unit from the old exponent.":stage==7||stage==10?"Similar example: d(2x^3 - 4x + 6)/dx = 6x^2 - 4. Differentiate each term separately before combining.":"Similar example: d(2x^3)/dx = (2 x 3)x^(3-1) = 6x^2. Apply the same two operations to your own term.";
            return stage==1?"Which part of a fractional exponent represents the root index?":stage==8?"How does moving a power from the denominator affect its exponent?":stage==6?"Does this term change when x changes?":stage==9?"Treat the linear term as a power with exponent one.":stage==7||stage==10?"Combine the derivatives of each term. Check the signs and the constant term separately.":"Identify this term's coefficient and exponent. Apply the two Power Rule operations separately.";
        }
        public static string Integration(int module,int step,bool commonConstant,int tier)
        {
            if(module==3)return tier==0?"Find an antiderivative first. Evaluate it at the upper bound and at the lower bound separately.":tier==1?BoundsRule:"Similar example: integrate 3x^2 from 1 to 2. F(x) = x^3, so F(2) - F(1) = 8 - 1 = 7.";
            if(module==2)
            {
                int s=System.Math.Min(step,3);
                if(tier==0)return new[]{"Start with the root. Which fractional power represents it?","Use your accepted rewritten exponent. What happens when you add one whole unit?","Use your accepted raised exponent as the divisor. The original coefficient is one.","You have completed the power calculations. What must be included in the general indefinite integral?"}[s];
                if(tier==1)return new[]{"The nth root of x^m is x^(m/n).","To add one to a fraction, express one using the same denominator.","The new coefficient is the old coefficient divided by the raised exponent.","Antiderivatives of the same function can differ by an arbitrary constant. Use one constant for the whole expression."}[s];
                return new[]{"Similar example: the fourth root of x is x^(1/4). The root index becomes the denominator.","Similar example: 1/4 + 1 = 1/4 + 4/4 = 5/4.","Similar example: dividing 1 by 5/4 gives 4/5. Use the reciprocal of the raised exponent.","Similar example: the general integral of 8x is 4x^2 + C. The constant represents all antiderivatives."}[s];
            }
            if(module==1&&commonConstant)return tier==0?"All three term connections are accepted. How do you represent the family of indefinite integrals?":tier==1?"An indefinite integral needs one arbitrary constant for the combined expression.":"Similar example: the integral of 6x + 4 is 3x^2 + 4x + C. Use one common constant after combining the terms.";
            if(module==1)
            {
                int source=System.Math.Max(0,System.Math.Min(step,2));
                if(tier==0)return new[]{"Focus on the first source term. Which output socket would differentiate back to this term?","Focus on the linear term. Its antiderivative should contain a higher power of x. Which socket matches this pattern?","This source term is a constant. Which output would produce that constant when differentiated?"}[source];
                if(tier==1)return new[]{"For a term a*x^n with n != -1, increase the exponent by one and divide the coefficient by the new exponent. Match this term first; add the common constant only after all connections.","For a linear term a*x, increase the exponent from 1 to 2 and divide the coefficient by 2. Match the result before adding the common constant.","The integral of a constant a is a*x + C. Preserve the sign of the constant, and add one common constant only after all terms are matched."}[source];
                return new[]{"Similar example: integrating 6x^2 produces 2x^3 before adding the constant. Match 6x^2 to 2x^3.","Similar example: integrating 8x produces 4x^2 before adding the constant. Match 8x to 4x^2.","Similar example: integrating -7 produces -7x before adding the constant. Match -7 to -7x."}[source];
            }
            return tier==0?"Raise the original exponent by one. That new exponent also determines the divisor; check the arbitrary constant.":tier==1?IntegralRule:"Similar example: integrate x^6 to get x^7/7 + C. The raised exponent supplies both the new power and the divisor.";
        }
    }
}
