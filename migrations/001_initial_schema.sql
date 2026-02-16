-- Migration: Initial Supabase Schema for DnD Damage Calculator
-- Run this in the Supabase SQL Editor

-- Create characters table with JSONB storage
CREATE TABLE IF NOT EXISTS characters (
  id BIGSERIAL PRIMARY KEY,
  user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
  name TEXT NOT NULL,
  data JSONB NOT NULL,
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Create index for user queries (most common access pattern)
CREATE INDEX IF NOT EXISTS idx_characters_user_id ON characters(user_id);

-- Optional: Create index on name for faster character list sorting
CREATE INDEX IF NOT EXISTS idx_characters_name ON characters(name);

-- Enable Row Level Security
ALTER TABLE characters ENABLE ROW LEVEL SECURITY;

-- RLS Policy: Users can only access their own characters
-- This policy applies to SELECT, INSERT, UPDATE, and DELETE
CREATE POLICY characters_user_isolation ON characters
  FOR ALL
  USING (auth.uid() = user_id)
  WITH CHECK (auth.uid() = user_id);

-- Optional: GIN index for JSONB queries (uncomment if needed for complex queries)
-- CREATE INDEX idx_characters_data ON characters USING GIN(data);

-- Add trigger to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
  NEW.updated_at = NOW();
  RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TRIGGER update_characters_updated_at BEFORE UPDATE ON characters
  FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Verify setup
SELECT 'Migration complete. Characters table created with RLS enabled.' AS status;
