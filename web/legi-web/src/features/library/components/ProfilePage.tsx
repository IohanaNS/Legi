import { ProfileExperience } from "../../profile/components/ProfileExperience";
import { useAuth } from "../../auth/useAuth";

export default function ProfilePage() {
  const { user } = useAuth();

  return <ProfileExperience targetUserId={user?.userId} />;
}
