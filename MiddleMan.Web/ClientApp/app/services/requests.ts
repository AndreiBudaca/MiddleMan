import { env } from "~/environment";

export async function get(url: string): Promise<Response | null> {
    const response = await fetch(url, {
        method: 'GET',
    });

    if (response.status == 401) {
        authenticate();
        return null;
    }

    return response;
}

function authenticate() {
    window.location.replace(`${env.API_BASE_URL}/account/login`);
}