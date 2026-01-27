using UnityEngine;

namespace TakoyakiPhysics.Meta
{
    public static class CommentGenerator
    {
        public static string GetComment(float totalScore, float cookLevel, float shapeIntegrity)
        {
            if (totalScore >= 95f)
            {
                return "..."; // Silent approval
            }
            
            if (cookLevel > 1.2f)
            {
                return "The burn... I will not make excuses.";
            }

            if (cookLevel < 0.8f)
            {
                return "The heat is insufficient.";
            }

            if (shapeIntegrity < 0.5f)
            {
                return "This is not a sphere. It is a tragedy.";
            }

            if (totalScore > 80f)
            {
                return "Polite movement. It tastes good before eating.";
            }

            return "Continue training.";
        }
    }
}
