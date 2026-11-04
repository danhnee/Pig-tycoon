import "jsr:@supabase/functions-js/edge-runtime.d.ts";
import { createClient } from "npm:@supabase/supabase-js@2";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers":
    "authorization, x-client-info, apikey, content-type",
};

function jsonResponse(body: Record<string, unknown>, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: {
      ...corsHeaders,
      "Content-Type": "application/json",
      "Connection": "keep-alive",
    },
  });
}

Deno.serve(async (req: Request) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }
  if (req.method !== "POST") {
    return jsonResponse({ msg: "Method not allowed." }, 405);
  }

  try {
    const payload = await req.json();
    const email = typeof payload?.email === "string"
      ? payload.email.trim().toLowerCase()
      : "";
    const password = typeof payload?.password === "string"
      ? payload.password
      : "";

    if (!email.includes("@") || password.length < 6) {
      return jsonResponse(
        { msg: "Nhập email hợp lệ và mật khẩu từ 6 ký tự." },
        400,
      );
    }

    const supabaseUrl = Deno.env.get("SUPABASE_URL") ?? "";
    let secretKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "";
    if (!secretKey) {
      const rawSecrets = Deno.env.get("SUPABASE_SECRET_KEYS");
      if (rawSecrets) {
        secretKey = JSON.parse(rawSecrets)["default"] ?? "";
      }
    }

    const adminClient = createClient(supabaseUrl, secretKey, {
      auth: { autoRefreshToken: false, persistSession: false },
    });

    const { error: createErr } = await adminClient.auth.admin.createUser({
      email,
      password,
      email_confirm: true,
    });
    if (createErr) {
      return jsonResponse(
        { msg: createErr.message },
        createErr.status ?? 400,
      );
    }

    let anonKey = Deno.env.get("SUPABASE_ANON_KEY") ?? "";
    if (!anonKey) {
      const rawPub = Deno.env.get("SUPABASE_PUBLISHABLE_KEYS");
      if (rawPub) {
        anonKey = JSON.parse(rawPub)["default"] ?? "";
      }
    }

    const publicClient = createClient(supabaseUrl, anonKey, {
      auth: { autoRefreshToken: false, persistSession: false },
    });

    const { data: signInData, error: signInErr } =
      await publicClient.auth.signInWithPassword({ email, password });

    if (signInErr || !signInData.session || !signInData.user) {
      return jsonResponse(
        { msg: signInErr?.message ?? "Không tạo được phiên đăng nhập." },
        400,
      );
    }

    return jsonResponse({
      access_token: signInData.session.access_token,
      user: {
        id: signInData.user.id,
        email: signInData.user.email,
      },
    });
  } catch (err) {
    const msg = err instanceof Error
      ? err.message
      : "Lỗi máy chủ khi tạo tài khoản.";
    return jsonResponse({ msg }, 500);
  }
});
