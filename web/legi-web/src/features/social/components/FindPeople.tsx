import { useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router-dom";
import { UserSearch } from "lucide-react";
import { Card } from "../../../components/ui/Card";
import { Button } from "../../../components/ui/Button";

// GUID (with or without dashes) — the backend has no username->userId lookup
// (no GET /identity/users/{username} exists), so discovery navigates by the
// user's id. You can grab an id from a feed card's actor link or a followers list.
const GUID_RE =
  /^[0-9a-f]{8}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{12}$/i;

export function FindPeople() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [value, setValue] = useState("");
  const [error, setError] = useState(false);

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    const id = value.trim();
    if (!GUID_RE.test(id)) {
      setError(true);
      return;
    }
    setError(false);
    navigate(`/users/${id}`);
  };

  return (
    <Card>
      <div className="p-4">
        <div className="mb-3 flex items-center gap-2 text-sm font-medium text-stone-700">
          <UserSearch size={16} />
          {t("feed.findPeople")}
        </div>

        <form onSubmit={handleSubmit} className="space-y-2">
          <input
            value={value}
            onChange={(e) => {
              setValue(e.target.value);
              setError(false);
            }}
            placeholder={t("feed.findPeoplePlaceholder")}
            className="w-full rounded-lg border border-stone-300 px-3 py-1.5 text-sm focus:outline-none focus:ring-1 focus:ring-green-600"
          />
          <Button type="submit" size="sm" className="w-full" disabled={!value.trim()}>
            {t("feed.findPeopleGo")}
          </Button>
        </form>

        {error && <p className="mt-2 text-xs text-red-600">{t("feed.userNotFound")}</p>}
        <p className="mt-2 text-xs text-stone-400">{t("feed.findPeopleHint")}</p>
      </div>
    </Card>
  );
}
