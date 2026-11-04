-- Wrap auth.uid() in a scalar subquery so PostgreSQL evaluates it once per statement (InitPlan)
-- instead of re-evaluating per row (Supabase advisor lint 0003_auth_rls_initplan).

alter policy "profiles: owner reads"   on public.profiles using ((select auth.uid()) = id);
alter policy "profiles: owner inserts" on public.profiles with check ((select auth.uid()) = id);
alter policy "profiles: owner updates" on public.profiles using ((select auth.uid()) = id) with check ((select auth.uid()) = id);

alter policy "save_slots: owner reads"   on public.save_slots using ((select auth.uid()) = user_id);
alter policy "save_slots: owner inserts" on public.save_slots with check ((select auth.uid()) = user_id);
alter policy "save_slots: owner updates" on public.save_slots using ((select auth.uid()) = user_id) with check ((select auth.uid()) = user_id);
