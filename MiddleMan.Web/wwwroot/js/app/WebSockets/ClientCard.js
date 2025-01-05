init();

function init() {
    const clientMethods = document.querySelectorAll("div.client-method");

    clientMethods.forEach(method => {
        const callButton = method.querySelector("button.call-btn");

        callButton.addEventListener("click", async () => {
            const methodArgs = Array.from(method.querySelectorAll("li.argument > div")).map(argElem => getArg(argElem));

            const targetUrl = method.dataset.targetUrl;
            const resp = await fetch(targetUrl, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(methodArgs)
            });

            if (!resp.ok) {
                alert("An error ocurred. Please try again");
            }

            if (resp.status === 204) {
                alert("Method called successfully");
                return;
            }

            const json = await resp.json();
            const resultContainer = method.querySelector("li.result > div");
            if (!resultContainer) {
                alert(`Function return a result, but none expected: ${json}`);
            }

            setArg(resultContainer, json);
        });
    });
}

function getArg(argElem) {
    if (argElem.classList.contains("array")) return getArrayArg(argElem);
    if (argElem.classList.contains("composite")) return getCompositeArg(argElem);
    if (argElem.classList.contains("primitive")) return getPrimiveArg(argElem);

    return null;
}

function getArrayArg(argElem) {
    return Array.from(argElem.querySelectorAll(":scope > div > ul > li > div > div > div")).map(el => getArg(el));
}

function getPrimiveArg(argElem) {
    const type = argElem.dataset.type;
    const nullable = argElem.dataset.nullable == 'True';
    const val = argElem.querySelector("input").value;

    if (val == '' && nullable) return null;

    return getTypedValue(val, type);
}

function getCompositeArg(argElem) {
    const obj = {};

    Array.from(argElem.querySelectorAll(":scope > ul > li > div")).forEach(el => {
        const elName = el.dataset.name;
        obj[elName] = getArg(el);
    });

    return obj;
}

function getTypedValue(val, type) {
    try {
        if (type == 'integer') return Number.parseInt(val);
        if (type == 'float') return Number.parseFloat(val);
        if (type == 'boolean') return val == 'True'
        return val;
    } catch {
        alert(`${val} cannot be converted into a ${type}`)
    }
}

function setArg(argElem, value) {
    try {
        if (argElem.classList.contains("array")) setArrayArg(argElem, value);
        if (argElem.classList.contains("composite")) setCompositeArg(argElem, value);
        if (argElem.classList.contains("primitive")) setPrimiveArg(argElem, value);
    } catch {
        alert(`${value} couldn't be mapped on the response representation`)
    }
}

function setArrayArg(argElem, value) {
    // Clear existing elements
    const elementsContainer = argElem.querySelector(":scope > div > ul");
    elementsContainer.textContent = '';

    // Add new ones for each element in the array
    value.forEach(() => argElem.clickFunction())

    // Set arguments
    Array.from(argElem.querySelectorAll(":scope > div > ul > li > div > div > div"))
        .forEach((el, index) => setArg(el, value[index]));
}

function setPrimiveArg(argElem, value) {
    const input = argElem.querySelector("input");
    input.value = value;
}

function setCompositeArg(argElem, value) {
    Array.from(argElem.querySelectorAll(":scope > ul > li > div")).forEach(el => {
        const elName = el.dataset.name;
        const normalizedName = elName.charAt(0).toLowerCase() + elName.slice(1);

        setArg(el, value[normalizedName]);
    });
}