import { useState } from "react";
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
} from "lucide-react";
import { Button } from "./ui/button";
import { Input } from "./ui/input";
import { Label } from "./ui/label";
import { Badge } from "./ui/badge";

export function Profile() {
  const [isEditing, setIsEditing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [profileData, setProfileData] = useState({
    fullName: "Alexander Petrov",
    email: "demo@financehub.com",
    phone: "+1 (555) 123-4567",
    location: "New York, USA",
    dateJoined: "January 15, 2024",
    accountType: "Premium",
    verified: true,
  });

  const [editData, setEditData] = useState({ ...profileData });

  const handleEdit = () => {
    setIsEditing(true);
    setEditData({ ...profileData });
  };

  const handleCancel = () => {
    setIsEditing(false);
    setEditData({ ...profileData });
  };

  const handleSave = () => {
    setIsSaving(true);
    // Simulate save
    setTimeout(() => {
      setProfileData({ ...editData });
      setIsEditing(false);
      setIsSaving(false);
    }, 1000);
  };

  const handleChange = (field: string, value: string) => {
    setEditData((prev) => ({ ...prev, [field]: value }));
  };

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Header */}
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

      <div className="grid gap-6">
        {/* Profile Card */}
        <div className="bg-card border border-border rounded-lg p-6">
          <div className="flex items-start gap-6">
            {/* Avatar */}
            <div className="relative group">
              <div className="w-24 h-24 rounded-full bg-gradient-to-br from-primary to-accent flex items-center justify-center text-3xl text-white">
                AP
              </div>
              {isEditing && (
                <button className="absolute inset-0 rounded-full bg-black/50 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity">
                  <Camera className="w-6 h-6 text-white" />
                </button>
              )}
            </div>

            {/* Info */}
            <div className="flex-1 space-y-4">
              <div className="flex items-center gap-3">
                <h2 className="text-2xl text-foreground">
                  {profileData.fullName}
                </h2>
                {profileData.verified && (
                  <Badge className="bg-success/10 text-success border-success/30">
                    <CheckCircle2 className="w-3 h-3 mr-1" />
                    Verified
                  </Badge>
                )}
                <Badge className="bg-accent/10 text-accent border-accent/30">
                  {profileData.accountType}
                </Badge>
              </div>

              <div className="grid grid-cols-2 gap-4 text-sm">
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Mail className="w-4 h-4" />
                  <span>{profileData.email}</span>
                </div>
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Phone className="w-4 h-4" />
                  <span>{profileData.phone}</span>
                </div>
                <div className="flex items-center gap-2 text-muted-foreground">
                  <MapPin className="w-4 h-4" />
                  <span>{profileData.location}</span>
                </div>
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Calendar className="w-4 h-4" />
                  <span>Member since {profileData.dateJoined}</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Edit Form */}
        {isEditing && (
          <div className="bg-card border border-border rounded-lg p-6 space-y-4">
            <h3 className="text-lg text-foreground mb-4">Edit profile</h3>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="fullName">Full name</Label>
                <div className="relative">
                  <User className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground" />
                  <Input
                    id="fullName"
                    value={editData.fullName}
                    onChange={(e) => handleChange("fullName", e.target.value)}
                    className="pl-10 bg-input-background"
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="email">Email</Label>
                <div className="relative">
                  <Mail className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground" />
                  <Input
                    id="email"
                    type="email"
                    value={editData.email}
                    onChange={(e) => handleChange("email", e.target.value)}
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
            </div>
          </div>
        )}

        {/* Security Section */}
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
                  Last changed 30 days ago
                </p>
              </div>
              <Button variant="outline" size="sm">
                Change
              </Button>
            </div>

            <div className="flex items-center justify-between py-3 border-b border-border last:border-0">
              <div>
                <p className="text-foreground">Two-factor authentication</p>
                <p className="text-sm text-muted-foreground">
                  Protect your account with 2FA
                </p>
              </div>
              <Button variant="outline" size="sm">
                Set up
              </Button>
            </div>

            <div className="flex items-center justify-between py-3">
              <div>
                <p className="text-foreground">Active sessions</p>
                <p className="text-sm text-muted-foreground">
                  Manage devices with account access
                </p>
              </div>
              <Button variant="outline" size="sm">
                View
              </Button>
            </div>
          </div>
        </div>

        {/* Activity Stats */}
        <div className="grid grid-cols-3 gap-4">
          <div className="bg-card border border-border rounded-lg p-4">
            <p className="text-sm text-muted-foreground mb-1">
              Transactions
            </p>
            <p className="text-2xl text-foreground">1,247</p>
            <p className="text-xs text-success mt-1">+12% this month</p>
          </div>

          <div className="bg-card border border-border rounded-lg p-4">
            <p className="text-sm text-muted-foreground mb-1">
              Total volume
            </p>
            <p className="text-2xl text-foreground">₽2.4M</p>
            <p className="text-xs text-success mt-1">+8% this month</p>
          </div>

          <div className="bg-card border border-border rounded-lg p-4">
            <p className="text-sm text-muted-foreground mb-1">
              Success rate
            </p>
            <p className="text-2xl text-foreground">98.5%</p>
            <p className="text-xs text-muted-foreground mt-1">
              Average value
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
