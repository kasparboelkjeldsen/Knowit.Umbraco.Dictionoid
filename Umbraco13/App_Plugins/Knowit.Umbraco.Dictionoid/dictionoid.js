(function (history) {
    // Create a function to dispatch a custom event
    var triggerUrlChangeEvent = function () {
        window.dispatchEvent(new CustomEvent('urlchange'));
    };

    // Wrap the pushState method to trigger your custom event
    var pushState = history.pushState;
    history.pushState = function (state) {
        pushState.apply(history, arguments);
        triggerUrlChangeEvent();
    };

    // Do the same for replaceState
    var replaceState = history.replaceState;
    history.replaceState = function (state) {
        replaceState.apply(history, arguments);
        triggerUrlChangeEvent();
    };

    // Add an event listener for the popstate event to handle browser navigation
    window.addEventListener('popstate', triggerUrlChangeEvent);
})(window.history);
window.addEventListener('urlchange', function () {
  if (window.location.href.includes("translation/dictionary/edit")) {
    setTimeout(isEdit,100);   
  }
  else if(window.location.href.includes("translation/dictionary/list")) {
    setTimeout(isList,100);
  }
});

if (window.location.href.includes("translation/dictionary/edit")) {
  setTimeout(isEdit,100);
}
else if(window.location.href.includes("translation/dictionary/list")) {
  setTimeout(isList,100);
}

function extractDictionaryItems(nodes) {
  let items = [];
  for (let i = 0; i < nodes.length; i++) {
    let item = {};
    item.key = nodes[i].querySelector('label').innerText;
    item.value = nodes[i].querySelector('textarea').value;
    item.id = nodes[i].querySelector('textarea').id,
    items.push(item);
  }
  return items;

}

function isEdit() {

  const pane = document.querySelector('.umb-pane');
  const translationProperties = document.querySelectorAll('[property="translation.property"]');
  if(!pane || !translationProperties || translationProperties.length == 0) 
  {
    setTimeout(isEdit,100);
    return;
  }
  
  const insertHtml = /*html*/`

  <div class="umb-pane">
    <div class="umb-pane-content">
      <div class="umb-pane-sub-views">
        <div class="umb-editor-sub-view">
          <div class="umb-editor-sub-view__content">
            <div class="umb-box ng-scope">
              <div class="umb-box-content dictionoid-box-content">
                <strong>
                    Dictionoid
                </strong>
                <em> - by Knowit</em>
                <hr>
                <textarea rows="2" class="autogrow w-100 dictionoid-color"style="height: 53px; min-height: 0px;" placeholder="You can color the translation here. For example: 'Keep it formal' or 'light and lose tone'"></textarea>
                <hr>
                <button class="dictionoid-button btn umb-button__button btn-warning umb-button-- umb-outline"> <span class="umb-button__content"> Translate missing fields </span> </button>
                
                <ul class="umb-load-indicator animated -half-second dictionoid-loading" style="display:none">
                  <li class="umb-load-indicator__bubble"></li>
                  <li class="umb-load-indicator__bubble"></li>
                  <li class="umb-load-indicator__bubble"></li>
                </ul>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
  `;
  
  pane.insertAdjacentHTML('afterend', insertHtml);

  document.querySelectorAll('.dictionoid-button').forEach(button => {
    button.addEventListener('click', function (e) {
      e.preventDefault();
      e.stopPropagation();
      const dictionary = extractDictionaryItems(translationProperties);
      button.disabled = true;

      document.querySelector('.dictionoid-loading').style.display = 'block';

      const payload = {
        color: document.querySelector('.dictionoid-color').value,
        items: dictionary
      }
      // Define the endpoint URL
      const apiUrl = '/umbraco/backoffice/dictionoid/translate';
      
      // Call API and output result to console
      fetch(apiUrl, {
        method: 'POST',     // Use POST method
        headers: {
          'Content-Type': 'application/json',
          // Include other headers as required, for example, authorization headers.
        },
        body: JSON.stringify(payload),  // Convert the JavaScript object to a JSON string
      })
      .then(response => {
        if (!response.ok) {
          throw new Error('Network response was not ok: ' + response.statusText);
        }
        return response.json();  // Assuming the server responds with JSON
      })
      .then(data => {
        data.Items.forEach((item, index) => {
          const id = dictionary[index].id;
          const translationProperty = document.querySelector(`#${id}`);
          translationProperty.value = item.Value;
          const event = new Event('input', {
            bubbles: true,
            cancelable: true,
          });
      
          translationProperty.dispatchEvent(event);
      
        });
      })
      .catch(error => {
        console.error('There was a problem with your fetch operation:', error);
      })
      .finally(() => {
        document.querySelector('.dictionoid-loading').style.display = 'none';  
        button.disabled = false;
      });
    });
  });

  const key = document.querySelector('#headerName').value;
  // fetch history
  fetch(`/umbraco/backoffice/dictionoid/history?key=${encodeURIComponent(key)}`).then(response => response.json()).then(data => {
    console.log('history',data);
    const dictionary = extractDictionaryItems(translationProperties);
    console.log('dictionary',dictionary)

    const changes = [];
    dictionary.forEach(item => {
      const history = data.find(d => d.languageCultureName === item.key);
      if (history && history.value != item.value) {
        
        const change = {
          key: item.key,
          current: item.value,
          history: history.value,
          timestamp: history.timestamp
        };
        changes.push(change);
      }
    });
    if(changes.length) {
      const historyHtml = changes.map(change => {
        return /*html*/`
        <hr />
        <strong>History</strong>
        <div style="margin-top:20px; border: 1px solid #ccc; padding: 10px; margin-bottom: 10px; border-radius: 5px; background-color: #f9f9f9;">
        <em>Timestamp: ${change.timestamp}</em> 
        <br />
        <strong style="color: #333; margin-bottom: 5px;">${change.key} value was:</strong>
        <span style="color: #555; margin: 0; padding: 0;"> ${change.history}</span>
        </div>
        `;
      }).join('');
      document.querySelector('.dictionoid-box-content').insertAdjacentHTML('beforeend', historyHtml);
    }
  });
  
}

function isList() {
  const pane = document.querySelector('.umb-pane');
  const thCheck = document.querySelectorAll('.table th')?.length > 1;
  
  if (!pane || !thCheck) {
    setTimeout(isList, 100);
    return;
  }
  fetch(`/umbraco/backoffice/dictionoid/clearcache`);
  const table = document.querySelector('.table');
  let languages = [];

  table.querySelectorAll('th').forEach((th, index) => {
    if (index > 0) {
      languages.push(th.innerText);
    }
  });
  
  loadScripts(() => {
    setTimeout(() => {
      tippy('.table td', {
        content: 'Loading...',
        placement: 'bottom-start',
        onShow(instance) {
          // Code here is executed every time the tippy shows
          const key = instance.reference.closest('tr').querySelector('th').innerText;
          const index = instance.reference.cellIndex-1;
          const language = languages[index]; // Assuming languages array is 0-based and your first th is not a language
          
          fetch(`/umbraco/backoffice/dictionoid/gettext?key=${encodeURIComponent(key)}`)
            .then(response => response.json())
            .then(data => {
              const item = data.find(d => d.languageCultureName === language);
              if (item && item.value && item.value.length) {
                instance.setContent(item.value);
              } else {
                instance.setContent('No translation available');
              }
            })
            .catch(error => {
              console.error('Error fetching translation:', error);
              instance.setContent('Error loading translation');
            });
        },
      });
    }, 200);
  });

  fetch(`/umbraco/backoffice/dictionoid/shouldcleanup`).then(response => response.json())
  .then(data => {
    if(data) {
      buildCleanup(pane);
    }
  });
}

function buildCleanup(pane) {
  const insertHtml = /*html*/`

  <div class="umb-pane dictionoid-cleanup">
    <div class="umb-pane-content">
      <div class="umb-pane-sub-views">
        <div class="umb-editor-sub-view">
          <div class="umb-editor-sub-view__content">
            <div class="umb-box ng-scope">
              <div class="umb-box-content">
                <strong>
                    Dictionoid
                </strong>
                <em> - by Knowit</em>
                <hr>
                <button class="dictionoid-cleanup-button btn umb-button__button btn-warning umb-button-- umb-outline"> <span class="umb-button__content"> Cleanup Dictionoid Fields </span> </button>
                <button class="dictionoid-cleanup-button-confirm btn umb-button__button btn-danger umb-button-- umb-outline" style="display:none"> <span class="umb-button__content"> Cleanup Dictionoid Fields (SURE?) </span> </button>
                <strong class="dictionoid-feedback"><em> - This will modify the code in your views!</em></strong>
                <ul class="umb-load-indicator animated -half-second dictionoid-loading" style="display:none">
                  <li class="umb-load-indicator__bubble"></li>
                  <li class="umb-load-indicator__bubble"></li>
                  <li class="umb-load-indicator__bubble"></li>
                </ul>
              
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
  `;
  
  pane.insertAdjacentHTML('beforebegin', insertHtml);

  document.querySelector('.dictionoid-cleanup-button').addEventListener('click', function (e) {
    e.preventDefault();
    e.stopPropagation();
    this.style.display = 'none';
    document.querySelector('.dictionoid-cleanup-button-confirm').style.display = null;

  });
  document.querySelector('.dictionoid-cleanup-button-confirm  ').addEventListener('click', function (e) {
    document.querySelector('.dictionoid-loading').style.display = 'block';
    e.preventDefault();
    e.stopPropagation();
    this.style.display = 'none';
    document.querySelector('.dictionoid-feedback').innerHTML = '<em>Your code might have been modified. If new keys have been created, you might need to restart Umbraco.</em>';
    fetch(`/umbraco/backoffice/dictionoid/cleanupinspect`).then(response => response.json())
    .then(data => {
      var list = buildNestedList(data);
      document.querySelector('.dictionoid-cleanup .umb-box-content').insertAdjacentHTML('beforeend', list);
    }).finally(() => {
      document.querySelector('.dictionoid-loading').style.display = 'none';  
    });
  });
}
function buildNestedList(changesObject) {
  // Start the main list
  let html = '<ul style="padding-top:20px;">';

  // Iterate over each file path in the object
  for (const [filePath, keys] of Object.entries(changesObject)) {
    // Add the file path as a list item
    html += `<li>${filePath}`;

    // Start a nested list for the keys
    html += '<ul>';

    // Add each key as a list item
    for (const key of keys) {
      html += `<li>${key}</li>`;
    }

    // Close the nested list
    html += '</ul>';

    // Close the file path list item
    html += '</li>';
  }

  // Close the main list
  html += '</ul>';

  return html;
}

function loadScripts(callback) {
    const popperScript = document.createElement('script');
    popperScript.src = 'https://cdn.jsdelivr.net/npm/@popperjs/core@2.11.8/dist/umd/popper.min.js';
    document.head.appendChild(popperScript);

    popperScript.onload = () => {
        const tippyScript = document.createElement('script');
        tippyScript.src = 'https://cdn.jsdelivr.net/npm/tippy.js@6.3.7/dist/tippy.umd.min.js';
        document.head.appendChild(tippyScript);

        const tippyCSS = document.createElement('link');
        tippyCSS.rel = 'stylesheet';
        tippyCSS.href = 'https://cdn.jsdelivr.net/npm/tippy.js@6.3.7/dist/tippy.min.css';
        document.head.appendChild(tippyCSS);

        tippyScript.onload = callback;
    };
}

