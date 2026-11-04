-- Pig Tycoon cloud save: one profile + one save slot per account.
-- Every table is protected by Row Level Security so an account can only
-- read and write its own rows.

create table if not exists public.profiles (
  id            uuid primary key references auth.users (id) on delete cascade,
  display_name  text not null default '',
  character     text not null default 'Khoa' check (character in ('An', 'Khoa')),
  farm_level    integer not null default 1 check (farm_level >= 0),
  gold          integer not null default 0,
  current_day   integer not null default 1 check (current_day >= 0),
  updated_at    timestamptz not null default now()
);

create table if not exists public.save_slots (
  user_id       uuid primary key references auth.users (id) on delete cascade,
  save_json     jsonb not null,
  save_version  integer not null,
  saved_at      timestamptz not null default now(),
  device        text not null default ''
);

alter table public.profiles   enable row level security;
alter table public.save_slots enable row level security;

drop policy if exists "profiles: owner reads"   on public.profiles;
drop policy if exists "profiles: owner inserts" on public.profiles;
drop policy if exists "profiles: owner updates" on public.profiles;
create policy "profiles: owner reads"   on public.profiles for select using (auth.uid() = id);
create policy "profiles: owner inserts" on public.profiles for insert with check (auth.uid() = id);
create policy "profiles: owner updates" on public.profiles for update using (auth.uid() = id) with check (auth.uid() = id);

drop policy if exists "save_slots: owner reads"   on public.save_slots;
drop policy if exists "save_slots: owner inserts" on public.save_slots;
drop policy if exists "save_slots: owner updates" on public.save_slots;
create policy "save_slots: owner reads"   on public.save_slots for select using (auth.uid() = user_id);
create policy "save_slots: owner inserts" on public.save_slots for insert with check (auth.uid() = user_id);
create policy "save_slots: owner updates" on public.save_slots for update using (auth.uid() = user_id) with check (auth.uid() = user_id);
