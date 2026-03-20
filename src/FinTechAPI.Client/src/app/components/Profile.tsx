import { useEffect, useMemo, useState } from "react";
import {
  User,
  Mail,
  Phone,
  Calendar,
  MapPin,
  Shield,
  Camera,
  Edit2,
  Save,
  X,
  CheckCircle2,
  AlertCircle,
} from "lucide-react";
import { Button } from "./ui/button";
import { Input } from "./ui/input";
import { Label } from "./ui/label";
import { Badge } from "./ui/badge";
import {
  getMyProfile,
  updateMyProfile,
  type ApiUpdateUserProfilePayload,
  type ApiUserProfile,
} from "../api/client";

export function Profile() {
  const [isEditing, setIsEditing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [profile, setProfile] = useState<ApiUserProfile | null>(null);
  const [editData, setEditData] = useState<ApiUpdateUserProfilePayload>({
    firstName: "",
    lastName: "",
    phone: "",
    location: "",
  });

  useEffect(() => {
    async function loadProfile() {
      try {
        setError(null);
        setIsLoading(true);
        const data = await getMyProfile();
        setProfile(data);
        setEditData({
          firstName: data.firstName,
          lastName: data.lastName,
          phone: data.phone,
          location: data.location,
        });
      } catch (requestError) {
        setError(
          requestError instanceof Error
            ? requestError.message
            : "Failed to load profile",
        );
      } finally {
        setIsLoading(false);
      }
    }

    loadProfile();
  }, []);

  const fullName = useMemo(() => {
    if (!profile) {
      return "";
    }

    return `${profile.firstName} ${profile.lastName}`.trim() || "Unnamed user";
  }, [profile]);

  const memberSince = useMemo(() => {
    if (!profile?.createdAt) {
      return "-";
    }

    const createdAt = new Date(profile.createdAt);
    if (Number.isNaN(createdAt.getTime())) {
      return "-";
    }

    return createdAt.toLocaleDateString("en-US", {
      year: "numeric",
      month: "long",
      day: "numeric",
    });
  }, [profile]);

  const initials = useMemo(() => {
    const source = `${profile?.firstName ?? ""} ${profile?.lastName ?? ""}`
      .trim()
      .split(" ")
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase() ?? "")
      .join("");

    return source || "U";
  }, [profile]);

  const handleEdit = () => {
    if (!profile) {
      return;
    }

    setIsEditing(true);
    setError(null);
    setEditData({
      firstName: profile.firstName,
      lastName: profile.lastName,
      phone: profile.phone,
      location: profile.location,
    });
  };

  const handleCancel = () => {
    if (!profile) {
      return;
    }

    setIsEditing(false);
    setError(null);
    setEditData({
      firstName: profile.firstName,
      lastName: profile.lastName,
      phone: profile.phone,
      location: profile.location,
    });
  };

  const handleSave = async () => {
    if (!profile) {
      return;
    }

    setIsSaving(true);
    setError(null);

    try {
      const updated = await updateMyProfile(editData);
      setProfile(updated);
      setIsEditing(false);
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "Failed to save profile",
      );
    } finally {
      setIsSaving(false);
    }
  };

  const handleChange = (
    field: keyof ApiUpdateUserProfilePayload,
    value: string,
  ) => {
    setEditData((prev) => ({ ...prev, [field]: value }));
  };

  if (isLoading) {
    return <div className="text-muted-foreground">Loading profile...</div>;
  }

  if (!profile) {
    return (
      <div className="space-y-4">
        <div className="bg-destructive/10 border border-destructive/30 rounded-lg p-4 flex items-start gap-3">
          <AlertCircle className="w-5 h-5 text-destructive flex-shrink-0 mt-0.5" />
          <p className="text-sm text-destructive">
            {error ?? "Profile is unavailable"}
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl text-foreground">Profile</h1>
          <p className="text-muted-foreground mt-1">
            Manage your account information
          </p>
        </div>

        {!isEditing ? (
          <Button onClick={handleEdit}>
            <Edit2 className="w-4 h-4 mr-2" />
            Edit
          </Button>
        ) : (
          <div className="flex gap-2">
            <Button variant="outline" onClick={handleCancel}>
              <X className="w-4 h-4 mr-2" />
              Cancel
            </Button>
            <Button onClick={handleSave} disabled={isSaving}>
              <Save className="w-4 h-4 mr-2" />
              {isSaving ? "Saving..." : "Save"}
            </Button>
          </div>
        )}
      </div>

      {error && (
        <div className="bg-destructive/10 border border-destructive/30 rounded-lg p-4 flex items-start gap-3">
          <AlertCircle className="w-5 h-5 text-destructive flex-shrink-0 mt-0.5" />
          <p className="text-sm text-destructive">{error}</p>
        </div>
      )}

      <div className="grid gap-6">
        <div className="bg-card border border-border rounded-lg p-6">
          <div className="flex items-start gap-6">
            <div className="relative group">
              <div className="w-24 h-24 rounded-full bg-gradient-to-br from-primary to-accent flex items-center justify-center text-3xl text-white">
                {initials}
              </div>
              {isEditing && (
                <button className="absolute inset-0 rounded-full bg-black/50 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity">
                  <Camera className="w-6 h-6 text-white" />
                </button>
              )}
            </div>

            <div className="flex-1 space-y-4">
              <div className="flex items-center gap-3 flex-wrap">
                <h2 className="text-2xl text-foreground">{fullName}</h2>
                {profile.emailVerified && (
                  <Badge className="bg-success/10 text-success border-success/30">
                    <CheckCircle2 className="w-3 h-3 mr-1" />
                    Verified
                  </Badge>
                )}
                <Badge className="bg-accent/10 text-accent border-accent/30">
                  {profile.role || "user"}
                </Badge>
              </div>

              <div className="grid grid-cols-2 gap-4 text-sm">
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Mail className="w-4 h-4" />
                  <span>{profile.email || "-"}</span>
                </div>
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Phone className="w-4 h-4" />
                  <span>{profile.phone || "-"}</span>
                </div>
                <div className="flex items-center gap-2 text-muted-foreground">
                  <MapPin className="w-4 h-4" />
                  <span>{profile.location || "-"}</span>
                </div>
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Calendar className="w-4 h-4" />
                  <span>Member since {memberSince}</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        {isEditing && (
          <div className="bg-card border border-border rounded-lg p-6 space-y-4">
            <h3 className="text-lg text-foreground mb-4">Edit profile</h3>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="firstName">First name</Label>
                <div className="relative">
                  <User className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground" />
                  <Input
                    id="firstName"
                    value={editData.firstName}
                    onChange={(e) => handleChange("firstName", e.target.value)}
                    className="pl-10 bg-input-background"
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="lastName">Last name</Label>
                <div className="relative">
                  <User className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground" />
                  <Input
                    id="lastName"
                    value={editData.lastName}
                    onChange={(e) => handleChange("lastName", e.target.value)}
                    className="pl-10 bg-input-background"
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="phone">Phone</Label>
                <div className="relative">
                  <Phone className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground" />
                  <Input
                    id="phone"
                    value={editData.phone}
                    onChange={(e) => handleChange("phone", e.target.value)}
                    className="pl-10 bg-input-background"
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="location">Location</Label>
                <div className="relative">
                  <MapPin className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground" />
                  <Input
                    id="location"
                    value={editData.location}
                    onChange={(e) => handleChange("location", e.target.value)}
                    className="pl-10 bg-input-background"
                  />
                </div>
              </div>

              <div className="space-y-2 col-span-2">
                <Label htmlFor="email">Email</Label>
                <div className="relative">
                  <Mail className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground" />
                  <Input
                    id="email"
                    type="email"
                    value={profile.email}
                    className="pl-10 bg-input-background"
                    disabled
                  />
                </div>
              </div>
            </div>
          </div>
        )}

        <div className="bg-card border border-border rounded-lg p-6">
          <div className="flex items-center gap-3 mb-4">
            <Shield className="w-5 h-5 text-accent" />
            <h3 className="text-lg text-foreground">Security</h3>
          </div>

          <div className="space-y-4">
            <div className="flex items-center justify-between py-3 border-b border-border last:border-0">
              <div>
                <p className="text-foreground">Password</p>
                <p className="text-sm text-muted-foreground">
                  Manage password in reset flow
                </p>
              </div>
            </div>

            <div className="flex items-center justify-between py-3">
              <div>
                <p className="text-foreground">Email verification</p>
                <p className="text-sm text-muted-foreground">
                  Verification status is synced from Firebase
                </p>
              </div>
              <Badge
                className={
                  profile.emailVerified
                    ? "bg-success/10 text-success border-success/30"
                    : "bg-secondary/40 text-muted-foreground border-border"
                }
              >
                {profile.emailVerified ? "Verified" : "Not verified"}
              </Badge>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
