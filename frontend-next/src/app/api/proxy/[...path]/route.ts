import type { NextRequest } from "next/server";
import { Agent } from "undici";

const API_URL = process.env.LETSTALK_API_URL ?? "https://127.0.0.1:5000";

// The local backend uses a self-signed certificate; only relax verification in development.
const dispatcher =
    process.env.NODE_ENV !== "production"
        ? new Agent({ connect: { rejectUnauthorized: false } })
        : undefined;

async function proxy(request: NextRequest, ctx: RouteContext<"/api/proxy/[...path]">) {
    const { path } = await ctx.params;
    const target = `${API_URL}/${path.join("/")}${request.nextUrl.search}`;

    const headers = new Headers();
    for (const name of ["authorization", "content-type", "accept"]) {
        const value = request.headers.get(name);
        if (value) headers.set(name, value);
    }

    const hasBody = request.method !== "GET" && request.method !== "HEAD";
    let upstream: Response;
    try {
        upstream = await fetch(target, {
            method: request.method,
            headers,
            body: hasBody ? await request.arrayBuffer() : undefined,
            // `dispatcher` is an undici extension to fetch's RequestInit.
            ...({ dispatcher } as object),
        });
    } catch (e) {
        console.error(`[proxy] ${request.method} ${target} failed:`, e);
        return Response.json(
            { title: "Backend unreachable", detail: String((e as Error).cause ?? e) },
            { status: 502 },
        );
    }

    return new Response(upstream.body, {
        status: upstream.status,
        headers: { "content-type": upstream.headers.get("content-type") ?? "application/json" },
    });
}

export { proxy as GET, proxy as POST, proxy as PUT, proxy as DELETE };
