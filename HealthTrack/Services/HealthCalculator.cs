using HealthTrack.Models;

namespace HealthTrack.Services
{
    /// <summary>Pure calculation helpers for health metrics.</summary>
    public static class HealthCalculator
    {
        /// <summary>BMI = weight (kg) / height (m)^2, rounded to one decimal place.</summary>
        public static double CalculateBmi(double weightKg, double heightCm)
        {
            if (weightKg <= 0 || heightCm <= 0) return 0;
            double heightM = heightCm / 100.0;
            return Math.Round(weightKg / (heightM * heightM), 1);
        }

        /// <summary>WHO adult BMI categories.</summary>
        public static string BmiCategory(double bmi)
        {
            if (bmi <= 0) return "Not available";
            if (bmi < 18.5) return "Underweight";
            if (bmi < 25) return "Healthy weight";
            if (bmi < 30) return "Overweight";
            return "Obese";
        }

        /// <summary>CSS modifier used to colour the BMI badge.</summary>
        public static string BmiTone(double bmi)
        {
            if (bmi <= 0) return "neutral";
            if (bmi < 18.5) return "warn";
            if (bmi < 25) return "good";
            if (bmi < 30) return "warn";
            return "alert";
        }

        /// <summary>
        /// Rough estimate used when the client does not enter calories burned.
        /// Approximate kcal per minute for an average adult: low 4, moderate 7, high 10.
        /// </summary>
        public static int EstimateCaloriesBurned(int durationMinutes, WorkoutIntensity intensity)
        {
            int perMinute = intensity switch
            {
                WorkoutIntensity.Low => 4,
                WorkoutIntensity.High => 10,
                _ => 7
            };
            return Math.Max(0, durationMinutes) * perMinute;
        }

        public static bool IsHighBloodPressure(int? systolic, int? diastolic) =>
            (systolic ?? 0) >= 140 || (diastolic ?? 0) >= 90;
    }
}
