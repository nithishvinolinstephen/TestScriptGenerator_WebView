# Troubleshooting - AI Response Not Generated

## Symptom
Clicked "Generate Script" in AI mode but got the default template code instead of AI-generated code.

## Quick Diagnosis

### Step 1: Check Status Bar
Look at the status message in the toolbar after generation:
- **"Script generated successfully (AI) for Selenium Java"** = AI worked ✅
- **"Script generated successfully (Deterministic) for Selenium Java"** = Fallback used ❌

### Step 2: Check Settings

1. Click **⚙ Settings** button
2. Verify:
   - **LLM Provider**: OpenAI (or Groq/Ollama if preferred)
   - **Model**: gpt-4 (or appropriate for your provider)
   - **Base URL**: https://api.openai.com/v1 (default for OpenAI)
   - **API Key**: Filled in (should be hidden/masked)
3. Click **"Test Connection"** button
   - Should see success message if configured correctly

### Step 3: Check UI Settings

1. **Mode Radio Buttons** (toolbar):
   - Make sure **"AI"** radio button is selected, NOT "Deterministic"
   
2. **Framework Selector** (toolbar):
   - Verify framework is selected (e.g., "Selenium Java")

3. **AI Actions Tab** (right panel):
   - Make sure you entered a prompt describing the test
   - Example: "Search for apple iphone and verify results"

4. **Element Selection**:
   - At least one element should be in the "Elements" tab
   - Example: `Search Box`, `Search Button`, etc.

## Common Issues & Solutions

### Issue 1: "No elements selected" Error
**Solution:**
1. Click "Select Element" button
2. Click on web page elements (search box, button, etc.)
3. Click "Select Element" again to deactivate
4. Elements should appear in "Elements" tab

### Issue 2: "Please describe what the test should do" Error
**Solution:**
1. Go to "AI Actions" tab
2. Enter a prompt like: "Search for apple iphone"
3. Don't leave it empty

### Issue 3: "API key required for AI mode" Error
**Solution:**
1. Click ⚙ Settings
2. Select provider (e.g., OpenAI)
3. Enter your API key
4. Click "Test Connection" - wait for success message
5. Click OK to save
6. Try generating again

### Issue 4: Getting Deterministic Code (Status shows "Deterministic")
**Possible Causes:**

a) **API key not working**
   - Test in Settings with "Test Connection" button
   - Try a fresh API key from provider
   - Check token limits (might be exceeded)

b) **AI response validation failing**
   - Generated code didn't match expected patterns
   - This should show a message with details
   - Check format of generated response

c) **Prompt too vague**
   - Try more specific: "Search for 'apple iphone' in search box and verify results contain iPhone"
   - Instead of: "search"

d) **Network issue**
   - Check internet connection
   - Try disabling VPN if using one
   - Check firewall rules

e) **Model mismatch**
   - Ensure model name matches provider
   - OpenAI: gpt-4, gpt-3.5-turbo, etc.
   - Groq: mixtral-8x7b-32768, etc.
   - Ollama: llama2, neural-chat, etc.

### Issue 5: Application Takes Too Long to Generate
**Possible Causes:**
- Network latency (normal: 5-30 seconds)
- Large prompt or complex elements
- API provider rate limiting

**Solution:**
- Wait longer (up to 1 minute)
- If takes >1 min, may indicate connection issue
- Click Cancel if needed, retry

### Issue 6: Generated Code has Syntax Errors
**Possible Causes:**
- AI generated incomplete code
- Framework mismatch
- Missing imports

**Solutions:**
1. **Simplify prompt**: Use shorter, clearer description
2. **Check framework**: Ensure selected framework is supported
3. **Add context**: "Generate Selenium Java test that navigates to [URL] and searches for X"
4. **Retry**: Sometimes retrying generates better code

## Logs & Debugging

### Enable Console Output
The application logs to console. If running from terminal, you'll see:
- `[Information] AI generation attempt 1 of 3`
- `[Warning] Code validation failed: ...`
- `[Information] Falling back to deterministic generation`

### What to Look For
- "Health check passed" - Good, API is accessible
- "Health check failed" - Network/API issue
- "Response parsing failed" - AI response not understood
- "Code validation failed" - Generated code didn't meet standards
- "AI generation attempt 2 of 3" - Retry happened (expected if 1st fails)

## Step-by-Step Test

To verify AI is working:

```
1. Navigate to https://www.google.com
2. Click "Select Element" button
3. Click on Google search box
4. Click "Select Element" again
5. Go to "AI Actions" tab
6. Type: "Search for GitHub and click search"
7. Select: Selenium Java
8. Click "Generate Script"
9. Wait 5-30 seconds
10. Check status bar - should say "AI" not "Deterministic"
11. Look at generated code - should be real Selenium code
```

## Still Not Working?

If you've tried all above steps:

1. **Share what you see**:
   - Status bar message exactly
   - Generated code (first few lines)
   - Framework selected
   - Prompt you entered

2. **Check Settings test result**:
   - Should show "Connection successful" or error message

3. **Verify API key**:
   - Is it from correct provider?
   - Has token balance? (check provider account)
   - No typos/spaces in key?

4. **Try deterministic mode**:
   - To rule out framework/selection issues
   - Select "Deterministic" mode
   - If this works, AI issue is isolated

5. **Try different framework**:
   - Maybe AI is better at different language
   - Try "Playwright TypeScript" instead of "Selenium Java"

## Contact & Support

If still having issues:
- Check [PHASE8_UPDATES.md](PHASE8_UPDATES.md) for workflow overview
- Check [AI_RESPONSE_FIX.md](AI_RESPONSE_FIX.md) for technical details
- Verify Settings are correctly configured
- Ensure elements are properly selected
