const picker=document.querySelector('#language');
let catalog={};
function applyLanguage(code){
 if(!catalog[code])code='en';
 document.documentElement.lang=code;
 document.querySelectorAll('[data-i]').forEach(element=>{
  const value=catalog[code]?.[element.dataset.i]??catalog.en?.[element.dataset.i];
  if(typeof value!=='string')return;
  if(['title','releaseTitle','releaseAside','formatTitle'].includes(element.dataset.i))element.innerHTML=value;
  else element.textContent=value;
 });
 picker.value=code;
 try{localStorage.setItem('language',code)}catch{}
}
fetch('translations.json').then(response=>{if(!response.ok)throw new Error('Translations unavailable');return response.json()}).then(data=>{
 catalog=data;
 let code=navigator.language.slice(0,2);
 try{code=localStorage.getItem('language')||code}catch{}
 applyLanguage(code);
}).catch(()=>{picker.disabled=true});
picker.addEventListener('change',()=>applyLanguage(picker.value));
