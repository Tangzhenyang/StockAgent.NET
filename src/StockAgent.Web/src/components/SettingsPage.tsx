/**
 * Shows first-version research settings that are fixed until provider configuration is wired.
 */
export function SettingsPage() {
  return (
    <section className="settingsPanel">
      <h2>设置</h2>
      <dl>
        <div>
          <dt>报告语言</dt>
          <dd>中文</dd>
        </div>
        <div>
          <dt>证据上限</dt>
          <dd>30</dd>
        </div>
        <div>
          <dt>数据源</dt>
          <dd>FakeProvider</dd>
        </div>
      </dl>
    </section>
  );
}
