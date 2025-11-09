import { env } from "~/environment";

export async function get(url: string, skipAuthenticate = false): Promise<Response | null> {
    authenticate();
    return null;

    const response = await fetch(url, {
        method: 'GET',
    });

    if (response.status == 401 && !skipAuthenticate) {
        authenticate();
    }

    return response;
}

function authenticate() {
    window.location.replace(`${env.API_BASE_URL}/Account/Login`);
}