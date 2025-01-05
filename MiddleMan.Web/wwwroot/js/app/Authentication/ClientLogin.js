init();

function init() {
  const submitButton = document.getElementById("client-token-submit");
  submitButton.addEventListener("click", async () => {
    await generateClientToken(submitButton.dataset.target);
  });
}

async function generateClientToken(targetUrl) {
  const clientName = document.getElementById("client-name").value;

  const response = await fetch(targetUrl, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ clientName }),
  });

  if (response.ok) {
    const token = await response.text();
    const tokenContainer = document.getElementById("client-token-response");
    tokenContainer.value = token;
  } else {
  }
}
