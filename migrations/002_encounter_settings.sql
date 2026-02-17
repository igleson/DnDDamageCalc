-- Migration: Encounter Settings table
-- Run this in the Supabase SQL Editor

CREATE TABLE IF NOT EXISTS encounter_settings (
  id BIGSERIAL PRIMARY KEY,
  user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
  name TEXT NOT NULL,
  data JSONB NOT NULL,
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_encounter_settings_user_id ON encounter_settings(user_id);
CREATE INDEX IF NOT EXISTS idx_encounter_settings_name ON encounter_settings(name);

ALTER TABLE encounter_settings ENABLE ROW LEVEL SECURITY;

CREATE POLICY encounter_settings_user_isolation ON encounter_settings
  FOR ALL
  USING (auth.uid() = user_id)
  WITH CHECK (auth.uid() = user_id);

CREATE TRIGGER update_encounter_settings_updated_at BEFORE UPDATE ON encounter_settings
  FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
