import { Navigate, useParams } from "react-router-dom";
import { ProfileExperience } from "../../profile/components/ProfileExperience";
import { useAuth } from "../../auth/useAuth";

export default function UserProfilePage() {
  const { userId } = useParams<{ userId: string }>();
  const { user } = useAuth();

  if (userId && user?.userId === userId) {
    return <Navigate to="/profile" replace />;
  }

  return <ProfileExperience targetUserId={userId} />;
}
